using Signunder.Core.Registry;

namespace Signunder.Core.Tests;

public class TenantIdentityTests
{
    [Theory]
    [InlineData("acme.com", "acme.com")]
    [InlineData("  https://www.Acme.com/about ", "acme.com")]
    [InlineData("images.acme.co.uk.", "images.acme.co.uk")]
    [InlineData("bücher.de", "xn--bcher-kva.de")]
    [InlineData("localhost", null)]
    [InlineData("not a domain", null)]
    [InlineData("acme..com", null)]
    [InlineData("", null)]
    public void A_typed_domain_becomes_a_lower_case_host_name(string input, string? domain)
    {
        Assert.Equal(domain, TenantIdentity.NormaliseDomain(input));
    }

    [Fact]
    public void An_identity_is_active_only_when_verified_approved_for_the_current_name_and_issued()
    {
        var identity = new TenantIdentity { Domain = "acme.com", Token = "t" };
        Assert.Equal("unverified", identity.Status("Acme Ltd"));

        identity.DomainVerifiedAt = DateTimeOffset.UtcNow;
        Assert.Equal("awaiting_review", identity.Status("Acme Ltd"));

        identity.ApprovedName = "Acme Ltd";
        identity.ApprovedAt = DateTimeOffset.UtcNow;
        identity.CertChainPem = "-----BEGIN CERTIFICATE-----";
        identity.KeyRef = "dev:local";
        Assert.Equal("active", identity.Status("Acme Ltd"));

        // Renaming the company needs a new approval.
        Assert.False(identity.IsActive("Acme Holdings"));
    }
}
