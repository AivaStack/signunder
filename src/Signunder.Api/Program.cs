using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Signunder.Api.Auth;
using Signunder.Api.Data;
using Signunder.Core.Registry;
using Signunder.Core.Verification;
using Signunder.Sign.V1;
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
    .ConfigureChannel(o =>
    {
        o.MaxSendMessageSize = MaxSidecarMessageBytes;
        o.MaxReceiveMessageSize = MaxSidecarMessageBytes;
    });
builder.Services.AddGrpcClient<Health.HealthClient>(o => o.Address = sidecarAddress);

builder.Services.AddDbContext<RegistryDbContext>(o => o
    .UseNpgsql(builder.Configuration.GetConnectionString("Postgres"))
    .UseSnakeCaseNamingConvention());

builder.Services
    .AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.SchemeName, null);
var authorization = builder.Services.AddAuthorizationBuilder();
foreach (var scope in ApiScopes.All)
{
    authorization.AddPolicy(scope, policy => policy.RequireClaim(ApiKeyAuthenticationHandler.ScopeClaim, scope));
}

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

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

// First slice of POST /v1/sign (SIGNUNDER.md §4.6): one image in, the signed image out,
// signed with the calling tenant's active key, and a registry record whose id comes back
// in a header. No rules yet.
app.MapPost("/v1/sign", async (
    IFormFile file,
    RegistryDbContext db,
    SignService.SignServiceClient sidecar,
    IConfiguration config,
    HttpContext http,
    CancellationToken ct) =>
{
    var tenantId = http.User.TenantId();
    var key = await db.TenantKeys
        .Where(k => k.TenantId == tenantId && k.Status == TenantKeyStatus.Active)
        .OrderByDescending(k => k.CreatedAt)
        .FirstOrDefaultAsync(ct);
    if (key is null)
    {
        return Results.Problem("The tenant has no active signing key.", statusCode: 503);
    }
    if (file.ContentType is not ("image/jpeg" or "image/png"))
    {
        return Results.Problem("Only image/jpeg and image/png are supported.", statusCode: 415);
    }

    await using var upload = file.OpenReadStream();
    var request = new SignRequest
    {
        Asset = await Google.Protobuf.ByteString.FromStreamAsync(upload, ct),
        MimeType = file.ContentType,
        ManifestDefinitionJson = JsonSerializer.Serialize(new
        {
            claim_generator_info = new[] { new { name = "SignUnder", version = "0.1.0" } },
            title = file.FileName,
            assertions = new[]
            {
                new
                {
                    label = "c2pa.actions",
                    data = new
                    {
                        actions = new[]
                        {
                            new
                            {
                                action = "c2pa.created",
                                digitalSourceType = "http://cv.iptc.org/newscodes/digitalsourcetype/trainedAlgorithmicMedia",
                            },
                        },
                    },
                },
            },
        }),
        Signer = new SignerRef
        {
            KeyRef = key.KmsKeyArn,
            Algorithm = key.Algorithm,
            CertChainPem = key.CertChainPem,
            TsaUrl = config["Signer:TsaUrl"] ?? "",
        },
    };

    try
    {
        var reply = await sidecar.SignAsync(request, deadline: DateTime.UtcNow.AddSeconds(30), cancellationToken: ct);

        var record = AssetRecord.ForSignedImage(
            key, file.ContentType, reply.SignedAsset.Span, reply.ManifestStore.Span,
            reply.ValidationReportJson, ToCore(reply.ValidationState), DateTimeOffset.UtcNow);
        db.AssetRecords.Add(record);
        await db.SaveChangesAsync(ct);

        http.Response.Headers["SignUnder-Record-Id"] = record.Id;
        http.Response.Headers["SignUnder-Manifest-Hash"] = record.ManifestHash;
        return Results.File(reply.SignedAsset.ToByteArray(), file.ContentType, file.FileName);
    }
    catch (RpcException e) when (e.StatusCode == StatusCode.InvalidArgument)
    {
        return Results.Problem(e.Status.Detail, statusCode: 400);
    }
    catch (RpcException e)
    {
        return Results.Problem($"Sign sidecar: {e.StatusCode}: {e.Status.Detail}", statusCode: 502);
    }
}).DisableAntiforgery().RequireAuthorization(ApiScopes.Sign);

// Phase 0 slice of POST /v1/verify (SIGNUNDER.md §4.6): what the asset's active manifest
// says and whether it still holds. Ingredients are not summarised yet; they are in the report.
app.MapPost("/v1/verify", async (
    IFormFile file,
    SignService.SignServiceClient sidecar,
    CancellationToken ct) =>
{
    if (file.ContentType is not ("image/jpeg" or "image/png"))
    {
        return Results.Problem("Only image/jpeg and image/png are supported.", statusCode: 415);
    }

    await using var upload = file.OpenReadStream();
    var request = new ValidateRequest
    {
        Asset = await Google.Protobuf.ByteString.FromStreamAsync(upload, ct),
        MimeType = file.ContentType,
    };

    try
    {
        var reply = await sidecar.ValidateAsync(request, deadline: DateTime.UtcNow.AddSeconds(30), cancellationToken: ct);
        var body = JsonSerializer.SerializeToNode(
            VerificationSummary.FromCrJson(reply.ValidationReportJson, ToCore(reply.ValidationState)), snakeCase)!.AsObject();
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
        return Results.Problem($"Sign sidecar: {e.StatusCode}: {e.Status.Detail}", statusCode: 502);
    }
}).DisableAntiforgery().RequireAuthorization(ApiScopes.Verify);

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
        ["validation_report"] = JsonNode.Parse(record.ValidationReport),
    });
}).RequireAuthorization(ApiScopes.RecordsRead);

app.Run();

static Signunder.Core.Verification.ValidationState ToCore(Signunder.Sign.V1.ValidationState state) => state switch
{
    Signunder.Sign.V1.ValidationState.Trusted => Signunder.Core.Verification.ValidationState.Trusted,
    Signunder.Sign.V1.ValidationState.Valid => Signunder.Core.Verification.ValidationState.Valid,
    _ => Signunder.Core.Verification.ValidationState.Invalid,
};

/// <summary>Exposed so the API tests can host the application.</summary>
public partial class Program;
