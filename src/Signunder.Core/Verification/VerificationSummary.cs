using System.Text.Json;

namespace Signunder.Core.Verification;

/// <summary>
/// What <c>POST /v1/verify</c> says about an asset's active manifest (docs/ARCHITECTURE.md §1.6),
/// read from the sidecar's crJSON report.
/// </summary>
public sealed record VerificationSummary(
    ValidationState ValidationState,
    string? ManifestLabel,
    string? SignedBy,
    IReadOnlyDictionary<string, string> CertSubject,
    string TrustStatus,
    DateTimeOffset? IssuedAt,
    IReadOnlyList<string> Actions,
    bool Altered,
    IReadOnlyList<string> Failures)
{
    /// <summary>
    /// Summarises the active manifest, which crJSON lists first.
    /// </summary>
    /// <exception cref="JsonException">The report is not crJSON with at least one manifest.</exception>
    public static VerificationSummary FromCrJson(string crJson, ValidationState state)
    {
        using var report = JsonDocument.Parse(crJson);
        if (!report.RootElement.TryGetProperty("manifests", out var manifests)
            || manifests.ValueKind != JsonValueKind.Array
            || manifests.GetArrayLength() == 0)
        {
            throw new JsonException("The report holds no manifests.");
        }
        var manifest = manifests[0];

        var subject = new Dictionary<string, string>();
        DateTimeOffset? issuedAt = null;
        if (manifest.TryGetProperty("signature", out var signature))
        {
            if (signature.TryGetProperty("certificateInfo", out var certificate)
                && certificate.TryGetProperty("subject", out var names))
            {
                foreach (var name in names.EnumerateObject())
                {
                    subject[name.Name] = name.Value.ToString();
                }
            }
            if (signature.TryGetProperty("timeStampInfo", out var timeStamp)
                && timeStamp.TryGetProperty("timestamp", out var time))
            {
                issuedAt = time.GetDateTimeOffset();
            }
        }

        var actions = new List<string>();
        if (manifest.TryGetProperty("assertions", out var assertions))
        {
            foreach (var assertion in assertions.EnumerateObject())
            {
                if (assertion.Name.StartsWith("c2pa.actions", StringComparison.Ordinal)
                    && assertion.Value.TryGetProperty("actions", out var list))
                {
                    actions.AddRange(list.EnumerateArray()
                        .Select(action => action.TryGetProperty("action", out var name) ? name.GetString() : null)
                        .OfType<string>());
                }
            }
        }

        var failures = new List<string>();
        if (manifest.TryGetProperty("validationResults", out var results)
            && results.TryGetProperty("failure", out var failed))
        {
            failures.AddRange(failed.EnumerateArray()
                .Select(status => status.TryGetProperty("code", out var code) ? code.GetString() : null)
                .OfType<string>());
        }

        return new VerificationSummary(
            state,
            ManifestLabel: manifest.TryGetProperty("label", out var label) ? label.GetString() : null,
            SignedBy: subject.GetValueOrDefault("O") ?? subject.GetValueOrDefault("CN"),
            CertSubject: subject,
            TrustStatus: state == ValidationState.Trusted ? "trusted" : "untrusted",
            IssuedAt: issuedAt,
            Actions: actions,
            // Hard-binding codes: assertion.dataHash.mismatch, .bmffHash.mismatch, .boxesHash.mismatch.
            Altered: failures.Any(code => code.EndsWith("Hash.mismatch", StringComparison.Ordinal)),
            Failures: failures);
    }
}
