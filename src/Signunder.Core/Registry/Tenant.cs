namespace Signunder.Core.Registry;

public sealed class Tenant
{
    public Guid Id { get; init; }
    public required string LegalName { get; set; }
    public required string Slug { get; set; }
    public string Region { get; set; } = "eu";
    public string Tier { get; set; } = "free";
    public DateTimeOffset CreatedAt { get; init; }

    public const int MaxNameLength = 120;

    /// <summary>
    /// A URL slug from a company name: lower-case ASCII letters and digits joined by single
    /// hyphens, at most 40 characters. Empty if the name has none of those.
    /// </summary>
    public static string SlugFrom(string name)
    {
        var slug = new System.Text.StringBuilder();
        foreach (var c in name.Normalize(System.Text.NormalizationForm.FormD).ToLowerInvariant())
        {
            if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                slug.Append(c);
            }
            else if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark
                && slug.Length > 0 && slug[^1] != '-')
            {
                slug.Append('-');
            }
        }
        var result = slug.ToString().Trim('-');
        return result.Length <= 40 ? result : result[..40].TrimEnd('-');
    }
}
