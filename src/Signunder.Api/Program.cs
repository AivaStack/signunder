using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Signunder.Api;
using Signunder.Api.Account;
using Signunder.Api.Auth;
using Signunder.Api.Data;
using Signunder.Api.Proxy;
using Signunder.Api.Signing;
using Signunder.Api.Verification;
using Signunder.Core.Registry;
using Signunder.Core.Verification;
using Signunder.Sign.V1;
using Signunder.Wm.V1;
using Grpc.Core;
using Grpc.Health.V1;

// Assets travel to the sidecar as unary gRPC bytes (sign.proto), so the 4 MB default
// is raised on both sides. Uploads stay under Kestrel's own 30 MB request limit.
const int MaxSidecarMessageBytes = 64 * 1024 * 1024;

var snakeCase = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
};

var builder = WebApplication.CreateBuilder(args);

var sidecarAddress = new Uri(builder.Configuration["Sidecar:Address"] ?? "http://localhost:50051");
builder.Services
    .AddGrpcClient<SignService.SignServiceClient>(o => o.Address = sidecarAddress)
    .ConfigurePrimaryHttpMessageHandler(() => InternalTls.Handler(builder.Configuration, sidecarAddress))
    .ConfigureChannel(o =>
    {
        o.MaxSendMessageSize = MaxSidecarMessageBytes;
        o.MaxReceiveMessageSize = MaxSidecarMessageBytes;
        o.ServiceConfig = InternalTls.RetryUnavailable();
        // A retry resends the image, so it must be kept for the length of the call.
        o.MaxRetryBufferPerCallSize = MaxSidecarMessageBytes;
        o.MaxRetryBufferSize = 4L * MaxSidecarMessageBytes;
    });
builder.Services
    .AddGrpcClient<Health.HealthClient>(o => o.Address = sidecarAddress)
    .ConfigurePrimaryHttpMessageHandler(() => InternalTls.Handler(builder.Configuration, sidecarAddress));

// The watermark worker is optional: without an address, outputs are signed unmarked.
Uri? watermarkUri = builder.Configuration["Watermark:Address"] is { Length: > 0 } watermarkAddress ? new Uri(watermarkAddress) : null;
InternalTls.EnsureConfigured(builder.Configuration, sidecarAddress, watermarkUri);
if (watermarkUri is not null)
{
    builder.Services
        .AddGrpcClient<WatermarkService.WatermarkServiceClient>(o => o.Address = watermarkUri)
        .ConfigurePrimaryHttpMessageHandler(() => InternalTls.Handler(builder.Configuration, watermarkUri))
        .ConfigureChannel(o =>
        {
            o.MaxSendMessageSize = MaxSidecarMessageBytes;
            o.MaxReceiveMessageSize = MaxSidecarMessageBytes;
            o.ServiceConfig = InternalTls.RetryUnavailable();
            o.MaxRetryBufferPerCallSize = MaxSidecarMessageBytes;
            o.MaxRetryBufferSize = 4L * MaxSidecarMessageBytes;
        });
}

builder.Services.AddDbContext<RegistryDbContext>(o => o
    .UseNpgsql(builder.Configuration.GetConnectionString("Postgres"),
        npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", RegistryDbContext.Schema))
    .UseSnakeCaseNamingConvention());

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

// Per API key (§4.6). Unauthenticated callers share a limit per address.
var perMinute = builder.Configuration.GetValue("RateLimit:PerKeyPerMinute", 300);
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy(RateLimits.PerKey, http => RateLimitPartition.GetFixedWindowLimiter(
        http.User.FindFirst("api_key_id")?.Value ?? $"ip:{http.Connection.RemoteIpAddress}",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = perMinute, Window = TimeSpan.FromMinutes(1) }));
    // Lower, because each public verify can run the sidecar and the watermark worker.
    var publicPerMinute = builder.Configuration.GetValue("RateLimit:PublicPerMinute", 30);
    o.AddPolicy(RateLimits.Public, http => RateLimitPartition.GetFixedWindowLimiter(
        $"ip:{http.Connection.RemoteIpAddress}",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = publicPerMinute, Window = TimeSpan.FromMinutes(1) }));
    o.OnRejected = (context, _) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        return ValueTask.CompletedTask;
    };
});
builder.Services.AddScoped<ImageSigner>();
builder.Services.AddScoped<Quotas>();
builder.Services.AddHttpClient(DomainVerifier.Client, http => http.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddScoped<DomainVerifier>();
builder.Services.AddSingleton(TimeProvider.System);
if (builder.Configuration["Media:ServiceUrl"] is { Length: > 0 } mediaUrl)
{
    builder.Services.AddSingleton<Amazon.S3.IAmazonS3>(_ => new Amazon.S3.AmazonS3Client(
        new Amazon.Runtime.BasicAWSCredentials(builder.Configuration["Media:AccessKey"], builder.Configuration["Media:SecretKey"]),
        new Amazon.S3.AmazonS3Config
        {
            ServiceURL = mediaUrl,
            ForcePathStyle = true,
            // Supabase Storage checks signatures against the project's region.
            AuthenticationRegion = builder.Configuration["Media:Region"],
            // Checksums only where S3 requires them; local S3 stand-ins reject the extras.
            RequestChecksumCalculation = Amazon.Runtime.RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = Amazon.Runtime.ResponseChecksumValidation.WHEN_REQUIRED,
        }));
    builder.Services.AddSingleton<IMediaStore, S3MediaStore>();
}
else
{
    builder.Services.AddSingleton<IMediaStore, NoMediaStore>();
}
builder.Services.AddProxy(builder.Configuration);
builder.Services.AddWebsiteCors(builder.Configuration);
builder.Services.AddRules();
builder.Services.AddHostedService<MediaSweep>();

builder.Services
    .AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.SchemeName, null)
    .AddSupabaseUsers(builder.Configuration);
var authorization = builder.Services.AddAuthorizationBuilder();
authorization.AddAccountPolicy();
foreach (var scope in ApiScopes.All)
{
    authorization.AddPolicy(scope, policy => policy.RequireClaim(ApiKeyAuthenticationHandler.ScopeClaim, scope));
}

// Behind a proxy that terminates TLS (Fly.io), take the original scheme from
// X-Forwarded-Proto so links such as media_url are https. The client address comes only
// from a header the proxy itself sets and callers cannot (ForwardedHeaders:ClientIpHeader,
// Fly-Client-IP on Fly.io), so rate limits and logs see the caller, not the proxy.
var behindTlsProxy = builder.Configuration.GetValue<bool>("ForwardedHeaders:Proto");
var clientIpHeader = builder.Configuration["ForwardedHeaders:ClientIpHeader"];
if (behindTlsProxy || !string.IsNullOrEmpty(clientIpHeader))
{
    builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.None;
        if (behindTlsProxy)
        {
            options.ForwardedHeaders |= Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
        }
        if (!string.IsNullOrEmpty(clientIpHeader))
        {
            options.ForwardedHeaders |= Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor;
            options.ForwardedForHeaderName = clientIpHeader;
        }
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

var app = builder.Build();

if (behindTlsProxy || !string.IsNullOrEmpty(clientIpHeader))
{
    app.UseForwardedHeaders();
}

if (!app.Environment.IsDevelopment())
{
    // Unhandled errors become a bare problem response: no stack trace, no exception text.
    app.UseExceptionHandler();
}
app.UseSecurityHeaders();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    // The API's description, for local tooling such as the OWASP ZAP scan.
    app.MapOpenApi();
}

if (app.Configuration.GetValue<bool>("Database:MigrateAndSeedOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await DevelopmentSeed.RunAsync(scope.ServiceProvider.GetRequiredService<RegistryDbContext>(), app.Configuration);
}

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

app.MapGet("/readyz", async (Health.HealthClient sidecar, CancellationToken ct) =>
{
    try
    {
        var reply = await sidecar.CheckAsync(
            new HealthCheckRequest { Service = "signunder.sign.v1.SignService" },
            deadline: DateTime.UtcNow.AddSeconds(2),
            cancellationToken: ct);
        return reply.Status == HealthCheckResponse.Types.ServingStatus.Serving
            ? Results.Ok(new { status = "ready" })
            : Results.Json(new { status = "not_ready", sidecar = reply.Status.ToString() }, statusCode: 503);
    }
    catch (RpcException e)
    {
        return Results.Json(new { status = "not_ready", sidecar = e.StatusCode.ToString() }, statusCode: 503);
    }
});

// First slice of POST /v1/sign (docs/ARCHITECTURE.md §1.6): one image in, the signed image out,
// watermarked and fingerprinted if the worker is configured, signed with the calling
// tenant's active key, and a registry record whose id comes back in a header. No rules yet.
app.MapPost("/v1/sign", async (IFormFile file, ImageSigner signer, HttpContext http, CancellationToken ct) =>
{
    await using var upload = file.OpenReadStream();
    var asset = await Google.Protobuf.ByteString.FromStreamAsync(upload, ct);

    var outcome = await signer.SignAsync(
        http.User.TenantId(), asset, file.ContentType ?? "", file.FileName, new SignOrigin(null, null), keepCopy: false, ct);
    if (!outcome.Succeeded)
    {
        return Results.Problem(outcome.Problem, statusCode: outcome.Status);
    }

    http.Response.Headers["SignUnder-Record-Id"] = outcome.Record!.Id;
    http.Response.Headers["SignUnder-Manifest-Hash"] = outcome.Record.ManifestHash;
    return Results.File(outcome.Signed!, file.ContentType, file.FileName);
}).DisableAntiforgery().RequireAuthorization(ApiScopes.Sign).RequireRateLimiting(RateLimits.PerKey);

// Phase 0 slice of POST /v1/verify (docs/ARCHITECTURE.md §1.6): what the asset's active manifest
// says and whether it still holds. Ingredients are not summarised yet; they are in the report.
app.MapPost("/v1/verify", async (
    IFormFile file,
    SignService.SignServiceClient sidecar,
    ILoggerFactory logs,
    CancellationToken ct) =>
{
    if (!ImageSigner.SupportedTypes.Contains(file.ContentType ?? ""))
    {
        return Results.Problem("Only image/jpeg, image/png and image/webp are supported.", statusCode: 415);
    }

    await using var upload = file.OpenReadStream();
    var request = new ValidateRequest
    {
        Asset = await Google.Protobuf.ByteString.FromStreamAsync(upload, ct),
        MimeType = file.ContentType,
    };

    try
    {
        var reply = await InternalTls.WithReconnect(() =>
            sidecar.ValidateAsync(request, deadline: DateTime.UtcNow.AddSeconds(30), cancellationToken: ct).ResponseAsync);
        var body = JsonSerializer.SerializeToNode(
            VerificationSummary.FromCrJson(reply.ValidationReportJson, ImageSigner.ToCore(reply.ValidationState)), snakeCase)!.AsObject();
        body.Insert(0, "manifest_found", true);
        body["report"] = JsonNode.Parse(reply.ValidationReportJson);
        return Results.Json(body);
    }
    catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
    {
        return Results.Json(new JsonObject { ["manifest_found"] = false });
    }
    catch (RpcException e) when (e.StatusCode == StatusCode.InvalidArgument)
    {
        return Results.Problem(e.Status.Detail, statusCode: 400);
    }
    catch (RpcException e)
    {
        var reference = ErrorReference.New();
        logs.CreateLogger("Signunder.Api.Verify").LogError(
            "Sign sidecar failed ({Code}): {Detail}. Reference {Reference}", e.StatusCode, e.Status.Detail, reference);
        return Results.Problem($"Verification failed. Quote reference {reference} to SignUnder support.", statusCode: 502);
    }
}).DisableAntiforgery().RequireAuthorization(ApiScopes.Verify).RequireRateLimiting(RateLimits.PerKey);

// Another tenant's record is reported as missing, not as forbidden.
app.MapGet("/v1/records/{id}", async (string id, HttpContext http, RegistryDbContext db, CancellationToken ct) =>
{
    var tenantId = http.User.TenantId();
    var record = await db.AssetRecords.AsNoTracking()
        .SingleOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, ct);
    if (record is null)
    {
        return Results.Problem($"No record {id}.", statusCode: 404);
    }

    return Results.Json(new JsonObject
    {
        ["id"] = record.Id,
        ["tenant_id"] = record.TenantId,
        ["created_at"] = record.CreatedAt,
        ["modality"] = record.Modality,
        ["mime"] = record.Mime,
        ["size"] = record.Size,
        ["bytes_sha256"] = record.BytesSha256,
        ["manifest_hash"] = record.ManifestHash,
        ["manifest_label"] = record.ManifestLabel,
        ["validation_state"] = JsonNamingPolicy.SnakeCaseLower.ConvertName(record.ValidationState.ToString()),
        ["actions"] = JsonNode.Parse(record.Actions),
        ["layers"] = JsonNode.Parse(record.Layers),
        // Hex, because the payload does not fit a JavaScript number.
        ["wm_payload"] = record.WmPayload?.ToString("x16"),
        ["fingerprint"] = record.Fingerprint is null ? null : Convert.ToHexStringLower(record.Fingerprint),
        ["fingerprint_quality"] = record.FingerprintQuality,
        ["provider"] = record.Provider,
        ["model_name"] = record.ModelName,
        ["media_url"] = record.RetainedMediaKey is null ? null : $"{http.Request.Scheme}://{http.Request.Host}/media/{record.Id}",
        ["validation_report"] = JsonNode.Parse(record.ValidationReport),
    });
}).RequireAuthorization(ApiScopes.RecordsRead).RequireRateLimiting(RateLimits.PerKey);

// Signed copies served by SignUnder itself (proxy mode). The record id in the URL is the
// only key: anyone holding the URL can fetch the file until the copy expires, as with
// the providers' own result URLs.
app.MapGet("/media/{id}", async (string id, RegistryDbContext db, IMediaStore media, CancellationToken ct) =>
{
    var key = await db.AssetRecords.AsNoTracking()
        .Where(r => r.Id == id && r.RetainedMediaKey != null)
        .Select(r => r.RetainedMediaKey)
        .SingleOrDefaultAsync(ct);
    var copy = key is null ? null : await media.GetAsync(key, ct);
    return copy is { } found ? Results.File(found.Content, found.ContentType) : Results.NotFound();
}).RequireRateLimiting(RateLimits.PerKey);

app.MapProxy();
app.MapPublicVerify();
app.MapRules();
app.MapAccount(app.Configuration);

app.Run();

/// <summary>Exposed so the API tests can host the application.</summary>
public partial class Program;
