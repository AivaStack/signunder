using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Signunder.Core.Registry;

public static class ApiScopes
{
    public const string Sign = "sign";
    public const string Verify = "verify";
    public const string RecordsRead = "records:read";

    public static readonly string[] All = [Sign, Verify, RecordsRead];
}

/// <summary>
/// A tenant's API key. Only its SHA-256 is stored; the key itself is shown once, when
/// it is made. Keys are 256 random bits, so a fast hash is enough.
/// </summary>
public sealed class ApiKey
{
    public const string LivePrefix = "su_live_";
    public const string TestPrefix = "su_test_";

    public Guid Id { get; init; }
    public Guid TenantId { get; init; }

    /// <summary>The first characters of the key, kept so a person can tell keys apart.</summary>
    public required string Prefix { get; init; }

    /// <summary>SHA-256 of the whole key, lower-case hex.</summary>
    public required string Hash { get; init; }
    public required string[] Scopes { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>Makes a new key and returns it with its plaintext, which is not stored.</summary>
    public static (ApiKey Key, string Plaintext) Generate(Guid tenantId, IEnumerable<string> scopes, DateTimeOffset now, bool live = true)
    {
        var plaintext = (live ? LivePrefix : TestPrefix) + Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        return (FromPlaintext(plaintext, tenantId, scopes, now), plaintext);
    }

    public static ApiKey FromPlaintext(string plaintext, Guid tenantId, IEnumerable<string> scopes, DateTimeOffset now)
    {
        if (!HasKeyShape(plaintext))
        {
            throw new ArgumentException("Not a SignUnder API key.", nameof(plaintext));
        }
        return new ApiKey
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Prefix = plaintext[..12],
            Hash = HashOf(plaintext),
            Scopes = [.. scopes],
            CreatedAt = now,
        };
    }

    public static string HashOf(string plaintext) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(plaintext)));

    /// <summary>A cheap check that saves a database lookup for things that cannot be keys.</summary>
    public static bool HasKeyShape(string candidate) =>
        candidate.Length is >= 40 and <= 100
        && (candidate.StartsWith(LivePrefix, StringComparison.Ordinal) || candidate.StartsWith(TestPrefix, StringComparison.Ordinal));
}
