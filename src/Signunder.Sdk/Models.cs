using System.Net;
using System.Text.Json;

namespace Signunder.Sdk;

/// <summary>A signed image and where its registry record is.</summary>
/// <param name="Content">The signed file. Serve these bytes unchanged; re-encoding breaks the signature.</param>
/// <param name="ContentType">The file's media type.</param>
/// <param name="RecordId">The registry record for this output.</param>
/// <param name="ManifestHash">SHA-256 of the C2PA manifest store, lower-case hex.</param>
public sealed record SignedAsset(byte[] Content, string ContentType, string RecordId, string ManifestHash);

/// <summary>What an image's Content Credentials say and whether they still hold.</summary>
/// <param name="ManifestFound">False if the image carries no Content Credentials; everything else is then empty.</param>
/// <param name="ValidationState"><c>invalid</c>, <c>valid</c> or <c>trusted</c>.</param>
/// <param name="ManifestLabel">The active manifest's identifier.</param>
/// <param name="SignedBy">The organisation, or failing that the common name, on the signing certificate.</param>
/// <param name="CertSubject">The signing certificate's subject fields.</param>
/// <param name="TrustStatus"><c>trusted</c> if the certificate chains to the C2PA trust list, otherwise <c>untrusted</c>.</param>
/// <param name="IssuedAt">The trusted timestamp on the signature, if it has one.</param>
/// <param name="Actions">The actions the manifest declares, for example <c>c2pa.created</c>.</param>
/// <param name="Altered">True if the image no longer matches what was signed.</param>
/// <param name="Failures">C2PA validation failure codes.</param>
/// <param name="Report">The full crJSON validation report.</param>
public sealed record Verification(
    bool ManifestFound,
    string? ValidationState = null,
    string? ManifestLabel = null,
    string? SignedBy = null,
    IReadOnlyDictionary<string, string>? CertSubject = null,
    string? TrustStatus = null,
    DateTimeOffset? IssuedAt = null,
    IReadOnlyList<string>? Actions = null,
    bool Altered = false,
    IReadOnlyList<string>? Failures = null,
    JsonElement? Report = null);

/// <summary>The registry's record of one signed output. It holds hashes, never the media.</summary>
/// <param name="Id">The record id.</param>
/// <param name="TenantId">The tenant that signed.</param>
/// <param name="CreatedAt">When it was signed.</param>
/// <param name="Modality">For example <c>image</c>.</param>
/// <param name="Mime">The signed file's media type.</param>
/// <param name="Size">The signed file's size in bytes.</param>
/// <param name="BytesSha256">SHA-256 of the signed file, lower-case hex.</param>
/// <param name="ManifestHash">SHA-256 of the C2PA manifest store, lower-case hex.</param>
/// <param name="ManifestLabel">The manifest's identifier.</param>
/// <param name="ValidationState">The state the output validated to when it was signed.</param>
/// <param name="Actions">The actions the manifest declares.</param>
/// <param name="Layers">Which marking layers the output carries, by name.</param>
/// <param name="ValidationReport">The crJSON report from validating the output after signing.</param>
/// <param name="WmPayload">The number carried by the output's invisible watermark, as 16 hex digits; null if it has none.</param>
/// <param name="Fingerprint">The output's PDQ perceptual hash, as 64 hex digits; null if it was not fingerprinted.</param>
/// <param name="FingerprintQuality">PDQ's 0 to 100 measure of how reliable the fingerprint is.</param>
public sealed record AssetRecord(
    string Id,
    Guid TenantId,
    DateTimeOffset CreatedAt,
    string Modality,
    string Mime,
    long Size,
    string BytesSha256,
    string ManifestHash,
    string? ManifestLabel,
    string ValidationState,
    IReadOnlyList<string> Actions,
    IReadOnlyDictionary<string, bool> Layers,
    JsonElement ValidationReport,
    string? WmPayload = null,
    string? Fingerprint = null,
    int? FingerprintQuality = null);

/// <summary>The SignUnder API refused a request.</summary>
public sealed class SignunderException(HttpStatusCode statusCode, string message) : Exception(message)
{
    /// <summary>The HTTP status the API answered with.</summary>
    public HttpStatusCode StatusCode { get; } = statusCode;
}
