namespace Signunder.Core.Registry;

/// <summary>
/// How a tenant pays (ADR 0015). The plan itself is <see cref="Tenant.Tier"/>, which the
/// quotas read; this holds the way to it: an upgrade request waiting for a hand-made
/// invoice, or the Stripe customer and subscription once card payment is on.
/// </summary>
public sealed class TenantBilling
{
    public Guid TenantId { get; init; }

    /// <summary>A plan asked for and not yet given, while billing is by invoice.</summary>
    public string? RequestedTier { get; set; }
    public string? RequestedInterval { get; set; }
    public string? RequestedCurrency { get; set; }
    public string? RequestedBy { get; set; }
    public DateTimeOffset? RequestedAt { get; set; }

    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }

    /// <summary>Stripe's status for the subscription, for example <c>active</c> or <c>past_due</c>.</summary>
    public string? SubscriptionStatus { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>The plans and the choices an upgrade takes.</summary>
public static class Plans
{
    public const string Free = "free";
    public const string Growth = "growth";
    public const string Business = "business";

    /// <summary>Plans bought from the dashboard; the others are sold by conversation.</summary>
    public static readonly IReadOnlySet<string> SelfServe = new HashSet<string> { Growth };

    public static readonly IReadOnlySet<string> Intervals = new HashSet<string> { "month", "year" };

    public static readonly IReadOnlySet<string> Currencies = new HashSet<string> { "eur", "usd" };

    /// <summary>
    /// Stripe subscription states in which the plan is kept. <c>past_due</c> keeps it while
    /// Stripe retries the card; the others end it.
    /// </summary>
    public static readonly IReadOnlySet<string> PaidStatuses = new HashSet<string> { "active", "trialing", "past_due" };
}
