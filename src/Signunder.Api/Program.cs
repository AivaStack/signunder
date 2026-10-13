using System.Text.Json;
using Signunder.Sign.V1;
using Grpc.Core;
using Grpc.Health.V1;

// Assets travel to the sidecar as unary gRPC bytes (sign.proto), so the 4 MB default
// is raised on both sides. Uploads stay under Kestrel's own 30 MB request limit.
const int MaxSidecarMessageBytes = 64 * 1024 * 1024;

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

var app = builder.Build();

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

// Phase 0 slice of POST /v1/sign (SIGNUNDER.md §4.6): one image in, the signed image out.
// No tenant auth, registry or rules yet; the signer comes from configuration.
app.MapPost("/v1/sign", async (
    IFormFile file,
    IConfiguration config,
    SignService.SignServiceClient sidecar,
    CancellationToken ct) =>
{
    var keyRef = config["Signer:KeyRef"];
    if (string.IsNullOrEmpty(keyRef))
    {
        return Results.Problem("No signer is configured.", statusCode: 503);
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
            KeyRef = keyRef,
            Algorithm = config["Signer:Algorithm"] ?? "es256",
            TsaUrl = config["Signer:TsaUrl"] ?? "",
        },
    };

    try
    {
        var reply = await sidecar.SignAsync(request, deadline: DateTime.UtcNow.AddSeconds(30), cancellationToken: ct);
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
}).DisableAntiforgery();

app.Run();
