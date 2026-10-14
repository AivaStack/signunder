using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string DemoKey = "su_test_api_tests_demo_tenant_key_000000000000";

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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Postgres", _postgres.GetConnectionString());
        builder.UseSetting("Database:MigrateAndSeedOnStartup", "true");
        builder.UseSetting("Signer:KeyRef", "dev:local");
        builder.UseSetting("Development:ApiKey", DemoKey);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<SignService.SignServiceClient>();
            services.AddSingleton<SignService.SignServiceClient>(Sidecar);
            services.AddSingleton<WatermarkService.WatermarkServiceClient>(Watermarker);
            services.RemoveAll<IMediaStore>();
            services.AddSingleton<IMediaStore>(Media);
            services.AddHttpClient(Signunder.Api.Proxy.ProxyEndpoints.Upstream)
                .ConfigurePrimaryHttpMessageHandler(() => Upstream);
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

    public override AsyncUnaryCall<SignResponse> SignAsync(SignRequest request, CallOptions options)
    {
        LastSigner = request.Signer;
        LastRequest = request;
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
/// fingerprint. A one-byte image is refused, as an unreadable one would be.
/// </summary>
public sealed class FakeWatermarker : WatermarkService.WatermarkServiceClient
{
    public static readonly byte[] Pdq = [.. Enumerable.Range(0, 32).Select(i => (byte)i)];

    public EmbedRequest? LastRequest { get; private set; }

    public override AsyncUnaryCall<EmbedResponse> EmbedAsync(EmbedRequest request, CallOptions options)
    {
        if (request.Asset.Length == 1)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "could not read the image"));
        }
        LastRequest = request;
        return FakeSidecar.Call(new EmbedResponse
        {
            WatermarkedAsset = ByteString.CopyFrom([.. request.Asset.Span, .. "[marked]"u8]),
            Algorithm = "com.adobe.trustmark.Q",
            Fingerprint = new Fingerprint { Pdq = ByteString.CopyFrom(Pdq), Quality = 87 },
        });
    }
}
