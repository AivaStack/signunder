using Signunder.Core.Registry;

namespace Signunder.Core.Tests;

public class ApiKeyTests
{
    [Fact]
    public void Generated_key_stores_a_hash_and_never_the_key()
    {
        var tenant = Guid.NewGuid();

        var (key, plaintext) = ApiKey.Generate(tenant, [ApiScopes.Sign], DateTimeOffset.UtcNow);

        Assert.StartsWith(ApiKey.LivePrefix, plaintext);
        Assert.True(ApiKey.HasKeyShape(plaintext));
        Assert.Equal(tenant, key.TenantId);
        Assert.Equal(ApiKey.HashOf(plaintext), key.Hash);
        Assert.Equal(64, key.Hash.Length);
        Assert.Equal(plaintext[..12], key.Prefix);
        Assert.DoesNotContain(plaintext[12..], key.Hash + key.Prefix);
        Assert.Equal([ApiScopes.Sign], key.Scopes);
    }

    [Fact]
    public void Keys_are_unique()
    {
        var first = ApiKey.Generate(Guid.NewGuid(), [], DateTimeOffset.UtcNow, live: false).Plaintext;
        var second = ApiKey.Generate(Guid.NewGuid(), [], DateTimeOffset.UtcNow, live: false).Plaintext;

        Assert.StartsWith(ApiKey.TestPrefix, first);
        Assert.NotEqual(first, second);
    }

    [Theory]
    [InlineData("")]
    [InlineData("su_live_short")]
    [InlineData("sk_live_0000000000000000000000000000000000000000000")]
    public void Other_strings_are_not_keys(string candidate)
    {
        Assert.False(ApiKey.HasKeyShape(candidate));
        Assert.Throws<ArgumentException>(() => ApiKey.FromPlaintext(candidate, Guid.NewGuid(), [], DateTimeOffset.UtcNow));
    }
}
