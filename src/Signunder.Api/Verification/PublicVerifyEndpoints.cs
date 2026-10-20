using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Signunder.Api.Data;
using Signunder.Api.Signing;
using Signunder.Core.Registry;
using Signunder.Core.Verification;
using Signunder.Sign.V1;
using Signunder.Wm.V1;

namespace Signunder.Api.Verification;

/// <summary>
/// The public verify page's data (docs/ARCHITECTURE.md §1.6): "was this made by Customer X,
/// when, with what, and has it been altered?" No API key. Answers only about the named
/// tenant's own outputs, found by manifest, by watermark, or by fingerprint when both are
/// stripped. Never says whether an image is AI-made in general (§1.3).
/// </summary>
public static class PublicVerifyEndpoints
{
    public static void MapPublicVerify(this IEndpointRouteBuilder app)
    {
        var routes = app.MapGroup("/public/v1/{tenant}")
            .RequireCors(Website.CorsPolicy)
            .RequireRateLimiting(RateLimits.Public);

        // The company's public name, so its verify page can greet visitors by it.
        routes.MapGet("", async (string tenant, RegistryDbContext db, CancellationToken ct) =>
        {
            if (await db.Tenants.AsNoTracking().SingleOrDefaultAsync(t => t.Slug == tenant, ct) is not { } owner)
            {
                return Results.Problem($"No company {tenant}.", statusCode: 404);
            }
            var view = Tenant(owner);
            var identity = await db.TenantIdentities.AsNoTracking().SingleOrDefaultAsync(i => i.TenantId == owner.Id, ct);
            if (identity is not null && identity.IsActive(owner.LegalName))
            {
                // Only once verified and approved (ADR 0014).
                view["verified_domain"] = identity.Domain;
            }
            return Results.Json(view);
        });

        routes.MapGet("/records/{id}", async (string tenant, string id, HttpContext http, RegistryDbContext db, CancellationToken ct) =>
        {
            var owner = await db.Tenants.AsNoTracking().SingleOrDefaultAsync(t => t.Slug == tenant, ct);
            var record = owner is null
                ? null
                : await db.AssetRecords.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id && r.TenantId == owner.Id, ct);
            return record is null
                ? Results.Problem($"{tenant} has no record {id}.", statusCode: 404)
                : Results.Json(PublicView(record, owner!, http));
        });

        routes.MapPost("/verify", VerifyAsync).DisableAntiforgery();
    }

    private static async Task<IResult> VerifyAsync(
        string tenant,
        IFormFile file,
        HttpContext http,
        RegistryDbContext db,
        SignService.SignServiceClient sidecar,
        IServiceProvider services,
        ILoggerFactory logs,
        CancellationToken ct)
    {
        var owner = await db.Tenants.AsNoTracking().SingleOrDefaultAsync(t => t.Slug == tenant, ct);
        if (owner is null)
        {
            return Results.Problem($"No tenant {tenant}.", statusCode: 404);
        }
        var mime = file.ContentType ?? "";
        if (!ImageSigner.SupportedTypes.Contains(mime))
        {
            return Results.Problem("Only image/jpeg, image/png and image/webp are supported.", statusCode: 415);
        }

        if (file.Length == 0)
        {
            return Results.Problem("The file is empty.", statusCode: 400);
        }

        await using var upload = file.OpenReadStream();
        var asset = await ByteString.FromStreamAsync(upload, ct);
        var log = logs.CreateLogger("Signunder.Api.PublicVerify");

        // 1. The manifest, if the image still carries one.
        VerificationSummary? manifest = null;
        try
        {
            var reply = await InternalTls.WithReconnect(() => sidecar.ValidateAsync(
                new ValidateRequest { Asset = asset, MimeType = mime },
                deadline: DateTime.UtcNow.AddSeconds(30), cancellationToken: ct).ResponseAsync);
            manifest = VerificationSummary.FromCrJson(reply.ValidationReportJson, ImageSigner.ToCore(reply.ValidationState));
        }
        catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
        {
        }
        catch (RpcException e) when (e.StatusCode == StatusCode.InvalidArgument)
        {
            return Unreadable(log, mime, e);
        }
        catch (RpcException e)
        {
            return Failed(log, "Sign sidecar", e);
        }

        var sha256 = Convert.ToHexStringLower(SHA256.HashData(asset.Span));
        var records = db.AssetRecords.AsNoTracking().Where(r => r.TenantId == owner.Id);
        AssetRecord? match = await records.FirstOrDefaultAsync(r => r.BytesSha256 == sha256, ct);
        if (match is null && manifest?.ManifestLabel is { } label)
        {
            match = await records.Where(r => r.ManifestLabel == label).OrderByDescending(r => r.CreatedAt).FirstOrDefaultAsync(ct);
        }
        var method = match is null ? null : "manifest";

        // 2 and 3. Without a manifest of theirs: the watermark, then the fingerprint.
        var checks = new JsonObject { ["manifest"] = true, ["watermark"] = false, ["fingerprint"] = false };
        int? distance = null;
        if (match is null && services.GetService<WatermarkService.WatermarkServiceClient>() is { } watermarker)
        {
            DecodeResponse decoded;
            try
            {
                decoded = await InternalTls.WithReconnect(() => watermarker.DecodeAsync(
                    new DecodeRequest { Asset = asset, MimeType = mime },
                    deadline: DateTime.UtcNow.AddSeconds(60), cancellationToken: ct).ResponseAsync);
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.InvalidArgument)
            {
                return Unreadable(log, mime, e);
            }
            catch (RpcException e)
            {
                return Failed(log, "Watermark worker", e);
            }

            checks["watermark"] = true;
            if (decoded.Present)
            {
                var payload = (long)decoded.Payload;
                match = await records.FirstOrDefaultAsync(r => r.WmPayload == payload, ct);
                method = match is null ? null : "watermark";
            }

            if (match is null && decoded.Fingerprint is { Quality: >= Pdq.MinQuality } fingerprint)
            {
                checks["fingerprint"] = true;
                // A scan of the tenant's fingerprints: fine at staging volumes. An indexed
                // search (§4.3) replaces it before tenants have many thousands of records.
                var candidates = await records
                    .Where(r => r.Fingerprint != null)
                    .Select(r => new { r.Id, r.Fingerprint })
                    .ToListAsync(ct);
                var nearest = candidates
                    .Select(c => (c.Id, Distance: Pdq.Distance(c.Fingerprint!, fingerprint.Pdq.Span)))
                    .OrderBy(c => c.Distance)
                    .FirstOrDefault();
                if (nearest.Id is not null && nearest.Distance <= Pdq.MatchDistance)
                {
                    match = await records.SingleAsync(r => r.Id == nearest.Id, ct);
                    method = "fingerprint";
                    distance = nearest.Distance;
                }
            }
        }

        return Results.Json(new JsonObject
        {
            ["tenant"] = Tenant(owner),
            ["ours"] = match is not null,
            ["method"] = method,
            // Bits out of 256 by which the image differs from the record's; 0 is identical.
            ["fingerprint_distance"] = distance,
            ["checks"] = checks,
            ["record"] = match is null ? null : PublicView(match, owner, http),
            ["manifest"] = manifest is null
                ? new JsonObject { ["found"] = false }
                : new JsonObject
                {
                    ["found"] = true,
                    ["validation_state"] = Snake(manifest.ValidationState),
                    ["signed_by"] = manifest.SignedBy,
                    ["trust_status"] = manifest.TrustStatus,
                    ["issued_at"] = manifest.IssuedAt,
                    ["actions"] = new JsonArray([.. manifest.Actions.Select(a => JsonValue.Create(a))]),
                    ["altered"] = manifest.Altered,
                },
        });
    }

    /// <summary>
    /// What anyone may see of a record. Leaves out what would help forge or strip a mark
    /// (the watermark payload, the fingerprint) and SignUnder's internal ids.
    /// </summary>
    private static JsonObject PublicView(AssetRecord record, Tenant owner, HttpContext http)
    {
        var signedBy = VerificationSummary.FromCrJson(record.ValidationReport, record.ValidationState);
        return new JsonObject
        {
            ["id"] = record.Id,
            ["tenant"] = Tenant(owner),
            ["created_at"] = record.CreatedAt,
            ["modality"] = record.Modality,
            ["mime"] = record.Mime,
            ["size"] = record.Size,
            ["bytes_sha256"] = record.BytesSha256,
            ["signed_by"] = signedBy.SignedBy,
            ["issued_at"] = signedBy.IssuedAt,
            ["validation_state"] = Snake(record.ValidationState),
            ["actions"] = JsonNode.Parse(record.Actions),
            ["layers"] = JsonNode.Parse(record.Layers),
            ["provider"] = record.Provider,
            ["model_name"] = record.ModelName,
            ["media_url"] = record.RetainedMediaKey is null ? null : $"{http.Request.Scheme}://{http.Request.Host}/media/{record.Id}",
            ["published_by"] = PublishedBy(record.PublishedBy),
        };
    }

    /// <summary>The company a CAWG identity assertion in the output names, and who verified it.</summary>
    private static JsonObject? PublishedBy(string? json)
    {
        if (json is null || JsonNode.Parse(json) is not JsonObject named)
        {
            return null;
        }
        named["verified_by"] = "SignUnder";
        return named;
    }

    private static JsonObject Tenant(Tenant owner) => new() { ["slug"] = owner.Slug, ["name"] = owner.LegalName };

    private static string Snake(Core.Verification.ValidationState state) => JsonNamingPolicy.SnakeCaseLower.ConvertName(state.ToString());

    /// <summary>The image could not be read; the libraries' own wording stays in the log.</summary>
    private static IResult Unreadable(ILogger log, string mime, RpcException e)
    {
        log.LogInformation("Unreadable upload ({Mime}): {Detail}", mime, e.Status.Detail);
        return Results.Problem(
            $"This file could not be read as {mime switch { "image/png" => "a PNG", "image/webp" => "a WebP", _ => "a JPEG" }} image. Check that it is a complete image of that type.",
            statusCode: 400);
    }

    private static IResult Failed(ILogger log, string service, RpcException e)
    {
        var reference = ErrorReference.New();
        log.LogError("{Service} failed ({Code}): {Detail}. Reference {Reference}", service, e.StatusCode, e.Status.Detail, reference);
        return Results.Problem($"Verification failed. Quote reference {reference} to SignUnder support.", statusCode: 502);
    }
}
