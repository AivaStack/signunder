using Google.Protobuf;
using Grpc.Core;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Signunder.Api.Account;
using Signunder.Api.Data;
using Signunder.Api.Signing;
using Signunder.Core.Registry;
using Signunder.Sign.V1;
using Signunder.Wm.V1;
using Testcontainers.PostgreSql;

namespace Signunder.Api.Tests;

/// <summary>
/// Hosts the API against a real Postgres in a container and a stand-in for the sidecar.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string DemoKey = "su_test_api_tests_demo_tenant_key_000000000000";

    /// <summary>The one origin allowed to call the public verify endpoints from a browser.</summary>
    public const string AllowedOrigin = "https://signunder.example";

    /// <summary>Stands in for the Supabase project's Auth issuer.</summary>
    public const string AuthIssuer = "https://auth.signunder.example/auth/v1";

    /// <summary>Stands in for the Supabase project's token signing key (ES256, as Supabase's).</summary>
    public static readonly ECDsaSecurityKey AuthKey = new(ECDsa.Create(ECCurve.NamedCurves.nistP256)) { KeyId = "test" };

    /// <summary>A Supabase Auth session token for a signed-in person.</summary>
    public static string UserToken(
        Guid userId,
        string email,
        string role = "authenticated",
        string audience = "authenticated",
        string issuer = AuthIssuer,
        SecurityKey? key = null,
        DateTime? expires = null) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Claims = new Dictionary<string, object> { ["sub"] = userId.ToString(), ["email"] = email, ["role"] = role },
            NotBefore = (expires ?? DateTime.UtcNow.AddHours(1)).AddHours(-2),
            Expires = expires ?? DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(key ?? AuthKey, SecurityAlgorithms.EcdsaSha256),
        });

    public HttpClient ClientWithUser(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return client;
    }

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18").Build();

    public FakeSidecar Sidecar { get; } = new();

    public FakeWatermarker Watermarker { get; } = new();

    public FakeUpstream Upstream { get; } = new();

    public InMemoryMediaStore Media { get; } = new();

    public Task InitializeAsync() => _postgres.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    /// <summary>Requests per minute per API key; tests that are not about the limit stay well under it.</summary>
    protected virtual int RateLimitPerMinute => 10_000;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("RateLimit:PerKeyPerMinute", RateLimitPerMinute.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Postgres", _postgres.GetConnectionString());
        builder.UseSetting("Database:MigrateAndSeedOnStartup", "true");
        builder.UseSetting("Signer:KeyRef", "dev:local");
        builder.UseSetting("Development:ApiKey", DemoKey);
        builder.UseSetting("Website:AllowedOrigins", AllowedOrigin);
        builder.UseSetting("SupabaseAuth:Issuer", AuthIssuer);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<SignService.SignServiceClient>();
            services.AddSingleton<SignService.SignServiceClient>(Sidecar);
            services.AddSingleton<WatermarkService.WatermarkServiceClient>(Watermarker);
            services.RemoveAll<IMediaStore>();
            services.AddSingleton<IMediaStore>(Media);
            services.AddHttpClient(Signunder.Api.Proxy.ProxyEndpoints.Upstream)
                .ConfigurePrimaryHttpMessageHandler(() => Upstream);
            // The issuer's keys come from AuthKey, not a discovery document.
            services.PostConfigure<JwtBearerOptions>(AccountEndpoints.Scheme, options =>
            {
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(
                    new OpenIdConnectConfiguration { Issuer = AuthIssuer });
                options.TokenValidationParameters.IssuerSigningKey = AuthKey;
            });
        });
    }

    public HttpClient ClientWithKey(string? apiKey)
    {
        var client = CreateClient();
        if (apiKey is not null)
        {
            client.DefaultRequestHeaders.Authorization = new("Bearer", apiKey);
        }
        return client;
    }

    /// <summary>Adds a tenant and returns an API key for it with the given scopes.</summary>
    public async Task<string> AddTenantAsync(string slug, string[] scopes, string? keyArn)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RegistryDbContext>();
        var now = DateTimeOffset.UtcNow;

        var tenant = new Tenant { Id = Guid.CreateVersion7(), LegalName = slug, Slug = slug, CreatedAt = now };
        db.Tenants.Add(tenant);
        if (keyArn is not null)
        {
            db.TenantKeys.Add(new TenantKey
            {
                Id = Guid.CreateVersion7(),
                TenantId = tenant.Id,
                KmsKeyArn = keyArn,
                Algorithm = "es256",
                CertChainPem = $"chain of {slug}",
                Status = TenantKeyStatus.Active,
                CreatedAt = now,
            });
        }
        var (apiKey, plaintext) = ApiKey.Generate(tenant.Id, scopes, now, live: false);
        db.ApiKeys.Add(apiKey);
        await db.SaveChangesAsync();
        return plaintext;
    }

    public async Task RevokeAsync(string apiKey)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RegistryDbContext>();
        var hash = ApiKey.HashOf(apiKey);
        var key = db.ApiKeys.Single(k => k.Hash == hash);
        key.RevokedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }
}

/// <summary>
/// Stands in for the sign sidecar: "signs" by appending a marker and answers with a real
/// report from docs/conformance/samples.
/// </summary>
public sealed class FakeSidecar : SignService.SignServiceClient
{
    private static readonly ByteString Marker = ByteString.CopyFromUtf8("[signed]");
    private static readonly string Report =
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "samples", "sample.jpg.crjson"));

    public SignerRef? LastSigner { get; private set; }

    public SignRequest? LastRequest { get; private set; }

    /// <summary>An asset starting with these bytes makes the sidecar fail with an internal error.</summary>
    public static readonly byte[] FailingAsset = "fail"u8.ToArray();

    public const string InternalDetail = "KMS Sign with arn:aws:kms:eu-central-1:1:key/secret-key-id failed";

    public override AsyncUnaryCall<SignResponse> SignAsync(SignRequest request, CallOptions options)
    {
        LastSigner = request.Signer;
        LastRequest = request;
        if (request.Asset.Span.StartsWith(FailingAsset))
        {
            throw new RpcException(new Status(StatusCode.Internal, InternalDetail));
        }
        return Call(new SignResponse
        {
            SignedAsset = ByteString.CopyFrom([.. request.Asset.Span, .. Marker.Span]),
            ManifestStore = ByteString.CopyFromUtf8("manifest store"),
            ValidationReportJson = Report,
            ValidationState = ValidationState.Valid,
        });
    }

    public override AsyncUnaryCall<ValidateResponse> ValidateAsync(ValidateRequest request, CallOptions options)
    {
        if (!request.Asset.Span.EndsWith(Marker.Span))
        {
            throw new RpcException(new Status(StatusCode.NotFound, "asset has no C2PA manifest"));
        }
        return Call(new ValidateResponse { ValidationReportJson = Report, ValidationState = ValidationState.Valid });
    }

    internal static AsyncUnaryCall<T> Call<T>(T reply) => new(
        Task.FromResult(reply), Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => [], () => { });
}

/// <summary>
/// Stands in for the watermark worker: "marks" by appending a marker and returns a fixed
/// fingerprint. A one-byte image is refused, as an unreadable one would be. Decoding finds
/// the payload of any image it marked, signed or not; an image starting with
/// <see cref="NearCopy"/> fingerprints within a few bits of the marked ones, and anything
/// else far from them.
/// </summary>
public sealed class FakeWatermarker : WatermarkService.WatermarkServiceClient
{
    public static readonly byte[] Pdq = [.. Enumerable.Range(0, 32).Select(i => (byte)i)];

    public static readonly byte[] NearCopy = "near copy"u8.ToArray();

    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, ulong> _payloads = new();

    public override AsyncUnaryCall<DecodeResponse> DecodeAsync(DecodeRequest request, CallOptions options)
    {
        if (request.Asset.Length == 1)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "could not read the image"));
        }
        var image = request.Asset.ToByteArray().AsSpan();
        if (image.EndsWith("[signed]"u8))
        {
            image = image[..^"[signed]"u8.Length];
        }
        ulong payload = 0;
        var marked = image.EndsWith("[marked]"u8)
            && _payloads.TryGetValue(Convert.ToBase64String(image[..^"[marked]"u8.Length]), out payload);
        byte[] pdq = marked ? Pdq
            : image.StartsWith(NearCopy) ? [(byte)(Pdq[0] ^ 0b111), .. Pdq[1..]]
            : [.. Pdq.Select(b => (byte)~b)];
        return FakeSidecar.Call(new DecodeResponse
        {
            Present = marked,
            Payload = payload,
            Fingerprint = new Fingerprint { Pdq = ByteString.CopyFrom(pdq), Quality = 87 },
        });
    }

    public EmbedRequest? LastRequest { get; private set; }

    public override AsyncUnaryCall<EmbedResponse> EmbedAsync(EmbedRequest request, CallOptions options)
    {
        if (request.Asset.Length == 1)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "could not read the image"));
        }
        LastRequest = request;
        _payloads[Convert.ToBase64String(request.Asset.ToByteArray())] = request.Payload;
        return FakeSidecar.Call(new EmbedResponse
        {
            WatermarkedAsset = ByteString.CopyFrom([.. request.Asset.Span, .. "[marked]"u8]),
            Algorithm = "com.adobe.trustmark.Q",
            Fingerprint = new Fingerprint { Pdq = ByteString.CopyFrom(Pdq), Quality = 87 },
        });
    }
}
