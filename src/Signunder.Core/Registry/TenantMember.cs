namespace Signunder.Core.Registry;

/// <summary>
/// A person who manages a tenant from the dashboard, identified by their Supabase Auth
/// user id. One tenant per person for now.
/// </summary>
public sealed class TenantMember
{
    public Guid TenantId { get; init; }

    /// <summary>The <c>sub</c> of the person's Supabase Auth token.</summary>
    public Guid UserId { get; init; }

    /// <summary>The address they signed in with, as Supabase Auth reported it then.</summary>
    public required string Email { get; init; }
    public string Role { get; init; } = "owner";
    public DateTimeOffset CreatedAt { get; init; }
}
