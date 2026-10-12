using Signunder.Sign.V1;
using Grpc.Core;
using Grpc.Health.V1;

var builder = WebApplication.CreateBuilder(args);

var sidecarAddress = new Uri(builder.Configuration["Sidecar:Address"] ?? "http://localhost:50051");
builder.Services.AddGrpcClient<SignService.SignServiceClient>(o => o.Address = sidecarAddress);
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

app.Run();
