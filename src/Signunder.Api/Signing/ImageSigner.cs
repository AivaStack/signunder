using System.Text.Json.Nodes;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Signunder.Api.Data;
using Signunder.Core.Registry;
using Signunder.Sign.V1;
using Signunder.Wm.V1;

namespace Signunder.Api.Signing;

/// <summary>Where a signed output came from, beyond the tenant.</summary>
/// <param name="Provider">For example <c>fal</c> or <c>replicate</c>; null for a direct upload.</param>
/// <param name="ModelName">The upstream model, as the provider names it.</param>
public sealed record SignOrigin(string? Provider, string? ModelName);

/// <summary>The signed bytes and their record, or why there are none.</summary>
public sealed record SignOutcome(byte[]? Signed, AssetRecord? Record, int Status, string? Problem)
{
    public bool Succeeded => Record is not null;

    public static SignOutcome Failed(int status, string problem) => new(null, null, status, problem);
}

/// <summary>
/// The sign pipeline shared by <c>/v1/sign</c> and proxy mode: watermark and fingerprint
/// (if the worker is configured), sign with the tenant's active key, validate, keep the
/// signed copy if asked, and write the registry record.
/// </summary>
public sealed class ImageSigner(
    RegistryDbContext db,
    SignService.SignServiceClient sidecar,
    IConfiguration config,
    IMediaStore media,
    IServiceProvider services)
{
    public static readonly IReadOnlySet<string> SupportedTypes =
        new HashSet<string> { "image/jpeg", "image/png", "image/webp" };

    public static string Extension(string mime) => mime switch
    {
        "image/jpeg" => "jpg",
        "image/png" => "png",
        "image/webp" => "webp",
        _ => throw new ArgumentOutOfRangeException(nameof(mime)),
    };

    public async Task<SignOutcome> SignAsync(
        Guid tenantId, ByteString asset, string mime, string title, SignOrigin origin, bool keepCopy, CancellationToken ct)
    {
        if (!SupportedTypes.Contains(mime))
        {
            return SignOutcome.Failed(415, "Only image/jpeg, image/png and image/webp are supported.");
        }
        var key = await db.TenantKeys
            .Where(k => k.TenantId == tenantId && k.Status == TenantKeyStatus.Active)
            .OrderByDescending(k => k.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (key is null)
        {
            return SignOutcome.Failed(503, "The tenant has no active signing key.");
        }

        // The watermark changes pixels, so it goes in before the manifest hard-binds them.
        Marking? marking = null;
        if (services.GetService<WatermarkService.WatermarkServiceClient>() is { } watermarker)
        {
            var payload = Marking.NewPayload();
            try
            {
                var embedded = await watermarker.EmbedAsync(
                    new EmbedRequest { Asset = asset, MimeType = mime, Payload = (ulong)payload },
                    deadline: DateTime.UtcNow.AddSeconds(30), cancellationToken: ct);
                asset = embedded.WatermarkedAsset;
                marking = new Marking(payload, embedded.Algorithm, embedded.Fingerprint.Pdq.ToByteArray(), embedded.Fingerprint.Quality);
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.InvalidArgument)
            {
                return SignOutcome.Failed(400, e.Status.Detail);
            }
            catch (RpcException e)
            {
                return SignOutcome.Failed(502, $"Watermark worker: {e.StatusCode}: {e.Status.Detail}");
            }
        }

        var request = new SignRequest
        {
            Asset = asset,
            MimeType = mime,
            ManifestDefinitionJson = ManifestDefinition(title, marking),
            Signer = new SignerRef
            {
                KeyRef = key.KmsKeyArn,
                Algorithm = key.Algorithm,
                CertChainPem = key.CertChainPem,
                TsaUrl = config["Signer:TsaUrl"] ?? "",
            },
        };

        SignResponse reply;
        try
        {
            reply = await sidecar.SignAsync(request, deadline: DateTime.UtcNow.AddSeconds(30), cancellationToken: ct);
        }
        catch (RpcException e) when (e.StatusCode == StatusCode.InvalidArgument)
        {
            return SignOutcome.Failed(400, e.Status.Detail);
        }
        catch (RpcException e)
        {
            return SignOutcome.Failed(502, $"Sign sidecar: {e.StatusCode}: {e.Status.Detail}");
        }

        var signed = reply.SignedAsset.ToByteArray();
        var record = AssetRecord.ForSignedImage(
            key, mime, signed, reply.ManifestStore.Span, reply.ValidationReportJson,
            ToCore(reply.ValidationState), DateTimeOffset.UtcNow, marking, origin.Provider, origin.ModelName);
        if (keepCopy)
        {
            record.RetainedMediaKey = $"{tenantId}/{record.Id}.{Extension(mime)}";
            await media.PutAsync(record.RetainedMediaKey, signed, mime, ct);
        }
        db.AssetRecords.Add(record);
        await db.SaveChangesAsync(ct);
        return new SignOutcome(signed, record, 200, null);
    }

    private static string ManifestDefinition(string title, Marking? marking)
    {
        var assertions = new JsonArray
        {
            new JsonObject
            {
                ["label"] = "c2pa.actions",
                ["data"] = new JsonObject
                {
                    ["actions"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["action"] = "c2pa.created",
                            ["digitalSourceType"] = "http://cv.iptc.org/newscodes/digitalsourcetype/trainedAlgorithmicMedia",
                        },
                    },
                },
            },
        };
        if (marking is not null)
        {
            // Ties the manifest to the watermark, so each can be found from the other (§4.7).
            assertions.Add(new JsonObject
            {
                ["label"] = "c2pa.soft-binding",
                ["data"] = new JsonObject
                {
                    ["alg"] = marking.Algorithm,
                    ["blocks"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["scope"] = new JsonObject(),
                            ["value"] = Convert.ToBase64String(Marking.PayloadBytes(marking.WatermarkPayload)),
                        },
                    },
                },
            });
        }
        return new JsonObject
        {
            ["claim_generator_info"] = new JsonArray { new JsonObject { ["name"] = "SignUnder", ["version"] = "0.1.0" } },
            ["title"] = title,
            ["assertions"] = assertions,
        }.ToJsonString();
    }

    public static Core.Verification.ValidationState ToCore(Sign.V1.ValidationState state) => state switch
    {
        Sign.V1.ValidationState.Trusted => Core.Verification.ValidationState.Trusted,
        Sign.V1.ValidationState.Valid => Core.Verification.ValidationState.Valid,
        _ => Core.Verification.ValidationState.Invalid,
    };
}
