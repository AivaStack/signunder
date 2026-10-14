using System.Text.Json;
using Signunder.Core.Verification;

namespace Signunder.Core.Tests;

/// <summary>Runs against the sidecar's real reports in docs/conformance/samples.</summary>
public class VerificationSummaryTests
{
    private static string Sample(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "samples", name));

    [Theory]
    [InlineData("sample.jpg.crjson")]
    [InlineData("sample.png.crjson")]
    [InlineData("sample.webp.crjson")]
    public void Signed_sample_is_summarised(string report)
    {
        var summary = VerificationSummary.FromCrJson(Sample(report), ValidationState.Valid);

        Assert.Equal("SignUnder DEV - NON-CONFORMANT", summary.SignedBy);
        Assert.Equal("SignUnder Development Signer (not trusted)", summary.CertSubject["CN"]);
        Assert.Equal("untrusted", summary.TrustStatus);
        Assert.Equal(2026, summary.IssuedAt?.Year);
        Assert.Equal(["c2pa.created", "c2pa.watermarked.bound"], summary.Actions);
        Assert.False(summary.Altered);
        Assert.Equal(["signingCredential.untrusted"], summary.Failures);
    }

    [Fact]
    public void Tampered_sample_is_altered()
    {
        var summary = VerificationSummary.FromCrJson(Sample("sample.tampered.jpg.crjson"), ValidationState.Invalid);

        Assert.True(summary.Altered);
        Assert.Contains("assertion.dataHash.mismatch", summary.Failures);
        Assert.Equal("untrusted", summary.TrustStatus);
    }

    [Fact]
    public void Trusted_state_is_reported_as_trusted()
    {
        var summary = VerificationSummary.FromCrJson(Sample("sample.jpg.crjson"), ValidationState.Trusted);

        Assert.Equal("trusted", summary.TrustStatus);
    }

    [Fact]
    public void Manifest_without_timestamp_or_subject_still_summarises()
    {
        var summary = VerificationSummary.FromCrJson("""{"manifests": [{"label": "x"}]}""", ValidationState.Valid);

        Assert.Null(summary.SignedBy);
        Assert.Null(summary.IssuedAt);
        Assert.Empty(summary.Actions);
        Assert.False(summary.Altered);
    }

    [Theory]
    [InlineData("""{}""")]
    [InlineData("""{"manifests": []}""")]
    public void Report_without_manifests_is_rejected(string report)
    {
        Assert.Throws<JsonException>(() => VerificationSummary.FromCrJson(report, ValidationState.Valid));
    }
}
