namespace Signunder.Core.Registry;

public sealed class Tenant
{
    public Guid Id { get; init; }
    public required string LegalName { get; set; }
    public required string Slug { get; set; }
    public string Region { get; set; } = "eu";
    public string Tier { get; set; } = "free";
    public DateTimeOffset CreatedAt { get; init; }
}
