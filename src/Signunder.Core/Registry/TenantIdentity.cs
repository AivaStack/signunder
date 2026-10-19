using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Signunder.Core.Registry;

/// <summary>
/// Who a tenant is, as SignUnder vouches for it in a CAWG identity assertion (ADR 0014):
/// a domain it proved control of by DNS, a name Eoin approved by hand, and the identity
/// certificate issued on both. Until all three exist the tenant signs without one.
/// </summary>
public sealed partial class TenantIdentity
{
    public const string TxtPrefix = "signunder-verification=";

    public Guid TenantId { get; init; }

    /// <summary>Lower-case host name, without a scheme or trailing dot.</summary>
    public required string Domain { get; set; }

    /// <summary>The value the tenant publishes in a TXT record on <see cref="Domain"/>.</summary>
    public required string Token { get; set; }

    public DateTimeOffset? DomainVerifiedAt { get; set; }

    /// <summary>The name as approved; a later change of the tenant's name needs a new approval.</summary>
    public string? ApprovedName { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }

    /// <summary>The identity certificate naming the tenant, first, then its issuers.</summary>
    public string? CertChainPem { get; set; }

    /// <summary>The key that signs identity assertions with that certificate.</summary>
    public string? KeyRef { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string TxtValue => TxtPrefix + Token;

    /// <summary>Whether signatures should carry the identity assertion for a tenant now called <paramref name="currentName"/>.</summary>
    public bool IsActive(string currentName) =>
        DomainVerifiedAt is not null
        && ApprovedName == currentName
        && !string.IsNullOrEmpty(CertChainPem)
        && !string.IsNullOrEmpty(KeyRef);

    /// <summary><c>unverified</c>, <c>awaiting_review</c> or <c>active</c>.</summary>
    public string Status(string currentName) =>
        DomainVerifiedAt is null ? "unverified" : IsActive(currentName) ? "active" : "awaiting_review";

    public static string NewToken() => Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));

    /// <summary>A host name from what a person typed (a bare domain or a URL), or null if it is not one.</summary>
    public static string? NormaliseDomain(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }
        var value = input.Trim().ToLowerInvariant();
        if (Uri.TryCreate(value.Contains("://", StringComparison.Ordinal) ? value : "https://" + value, UriKind.Absolute, out var uri))
        {
            value = uri.IdnHost;
        }
        value = value.TrimEnd('.');
        if (value.StartsWith("www.", StringComparison.Ordinal))
        {
            value = value[4..];
        }
        return value.Length <= 253 && Hostname().IsMatch(value) ? value : null;
    }

    [GeneratedRegex("^(?=.{1,253}$)([a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?\\.)+[a-z]([a-z0-9-]{0,61}[a-z0-9])?$", RegexOptions.CultureInvariant)]
    private static partial Regex Hostname();
}
