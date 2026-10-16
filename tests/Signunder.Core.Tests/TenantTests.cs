using Signunder.Core.Registry;

namespace Signunder.Core.Tests;

public class TenantTests
{
    [Theory]
    [InlineData("Acme Images Ltd.", "acme-images-ltd")]
    [InlineData("  Ünïcödé   Pictures!! ", "unicode-pictures")]
    [InlineData("Ørsted & Co", "rsted-co")]
    [InlineData("株式会社", "")]
    [InlineData("A very long company name that keeps going and going and going", "a-very-long-company-name-that-keeps-goin")]
    public void Slug_is_lower_case_ascii_joined_by_single_hyphens(string name, string slug)
    {
        Assert.Equal(slug, Tenant.SlugFrom(name));
    }
}
