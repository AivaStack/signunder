using System.Security.Cryptography;
using System.Text.Json;
using Signunder.Core.Verification;

namespace Signunder.Core.Registry;

/// <summary>
/// One signed output. Holds hashes and the manifest's description, never the media
/// (docs/ARCHITECTURE.md §1.1, principle 3).
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

    /// <summary>The 61-bit number carried by the output's invisible watermark, if it has one.</summary>
    public long? WmPayload { get; init; }

    /// <summary>The output's 256-bit PDQ perceptual hash, if it was fingerprinted.</summary>
    public byte[]? Fingerprint { get; init; }

    /// <summary>PDQ's 0 to 100 measure of how reliable <see cref="Fingerprint"/> is.</summary>
    public int? FingerprintQuality { get; init; }

    /// <summary>Where the output came from in proxy mode, for example <c>fal</c>; null for a direct upload.</summary>
    public string? Provider { get; init; }

    /// <summary>The upstream model, as the provider names it.</summary>
    public string? ModelName { get; init; }

    /// <summary>
    /// Where SignUnder keeps a copy of the signed output, if it serves the file itself
    /// (proxy mode). The copy expires; the record does not.
    /// </summary>
    public string? RetainedMediaKey { get; set; }

    /// <summary>
    /// The company a CAWG identity assertion in the output names, as JSON
    /// (<c>{"name": …, "domain": …}</c>); null if the output carries none (ADR 0014).
    /// </summary>
    public string? PublishedBy { get; set; }

    public static AssetRecord ForSignedImage(
        TenantKey key,
        string mime,
        ReadOnlySpan<byte> signedAsset,
        ReadOnlySpan<byte> manifestStore,
        string validationReport,
        ValidationState state,
        DateTimeOffset now,
        Marking? marking = null,
        string? provider = null,
        string? modelName = null)
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
            Layers = JsonSerializer.Serialize(new { manifest = true, watermark = marking is not null, fingerprint = marking is not null }),
            ValidationReport = validationReport,
            WmPayload = marking?.WatermarkPayload,
            Fingerprint = marking?.Pdq,
            FingerprintQuality = marking?.PdqQuality,
            Provider = provider,
            ModelName = modelName,
        };
    }
}

/// <summary>What the watermark worker did to an output before it was signed.</summary>
/// <param name="WatermarkPayload">The number embedded in the pixels; below 2^61.</param>
/// <param name="Algorithm">The soft-binding algorithm name, for example <c>com.adobe.trustmark.Q</c>.</param>
/// <param name="Pdq">The 32-byte PDQ hash of the watermarked image.</param>
/// <param name="PdqQuality">PDQ's 0 to 100 quality measure.</param>
public sealed record Marking(long WatermarkPayload, string Algorithm, byte[] Pdq, int PdqQuality)
{
    public const int PayloadBits = 61;

    /// <summary>A fresh random payload.</summary>
    public static long NewPayload() =>
        (long)(BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(8)) >> (64 - PayloadBits));

    /// <summary>The payload as the eight big-endian bytes recorded in the manifest's soft binding.</summary>
    public static byte[] PayloadBytes(long payload)
    {
        var bytes = new byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(bytes, payload);
        return bytes;
    }
}
