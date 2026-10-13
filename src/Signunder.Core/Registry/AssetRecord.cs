using System.Security.Cryptography;
using System.Text.Json;
using Signunder.Core.Verification;

namespace Signunder.Core.Registry;

/// <summary>
/// One signed output. Holds hashes and the manifest's description, never the media
/// (SIGNUNDER.md §4.1, principle 3).
/// </summary>
public sealed class AssetRecord
{
    /// <summary>A ULID, so records sort by creation time.</summary>
    public required string Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid TenantKeyId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public required string Modality { get; init; }
    public required string Mime { get; init; }
    public long Size { get; init; }

    /// <summary>SHA-256 of the signed asset, lower-case hex.</summary>
    public required string BytesSha256 { get; init; }

    /// <summary>SHA-256 of the C2PA manifest store, lower-case hex.</summary>
    public required string ManifestHash { get; init; }
    public string? ManifestLabel { get; init; }
    public ValidationState ValidationState { get; init; }

    /// <summary>JSON array of the manifest's action names.</summary>
    public required string Actions { get; init; }

    /// <summary>JSON object saying which marking layers the output carries.</summary>
    public required string Layers { get; init; }

    /// <summary>The sidecar's crJSON report from validating the output after signing.</summary>
    public required string ValidationReport { get; init; }

    public static AssetRecord ForSignedImage(
        TenantKey key,
        string mime,
        ReadOnlySpan<byte> signedAsset,
        ReadOnlySpan<byte> manifestStore,
        string validationReport,
        ValidationState state,
        DateTimeOffset now)
    {
        var summary = VerificationSummary.FromCrJson(validationReport, state);
        return new AssetRecord
        {
            Id = Ulid.NewUlid(now).ToString(),
            TenantId = key.TenantId,
            TenantKeyId = key.Id,
            CreatedAt = now,
            Modality = "image",
            Mime = mime,
            Size = signedAsset.Length,
            BytesSha256 = Convert.ToHexStringLower(SHA256.HashData(signedAsset)),
            ManifestHash = Convert.ToHexStringLower(SHA256.HashData(manifestStore)),
            ManifestLabel = summary.ManifestLabel,
            ValidationState = state,
            Actions = JsonSerializer.Serialize(summary.Actions),
            Layers = """{"manifest": true, "watermark": false, "fingerprint": false}""",
            ValidationReport = validationReport,
        };
    }
}
