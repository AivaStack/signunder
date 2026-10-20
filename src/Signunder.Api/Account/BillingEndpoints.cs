using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Signunder.Api.Data;
using Signunder.Core.Registry;

namespace Signunder.Api.Account;

/// <summary>
/// Upgrading from the dashboard (ADR 0015). With Stripe off, an upgrade is a request that
/// Eoin invoices and grants with <c>tools/set-plan.py</c>; with it on, the same call returns
/// a Checkout page, and Stripe's webhooks set the plan.
/// </summary>
public static class BillingEndpoints
{
    public sealed record UpgradeRequest(string? Tier, string? Interval, string? Currency);

    /// <summary>The account's billing, for <c>GET /v1/account</c>.</summary>
    public static JsonObject View(Tenant tenant, TenantBilling? billing, StripeBilling stripe) => new()
    {
        ["plan"] = tenant.Tier,
        // How an upgrade is paid: by card through Stripe, or by invoice.
        ["payment"] = stripe.Enabled ? "card" : "invoice",
        ["requested"] = billing?.RequestedTier is null ? null : new JsonObject
        {
            ["tier"] = billing.RequestedTier,
            ["interval"] = billing.RequestedInterval,
            ["currency"] = billing.RequestedCurrency,
            ["requested_at"] = billing.RequestedAt,
        },
        ["subscription_status"] = billing?.SubscriptionStatus,
        ["can_manage"] = stripe.Enabled && billing?.StripeCustomerId is not null,
    };

    public static void MapBilling(this RouteGroupBuilder account)
    {
        account.MapPost("/upgrade", async (UpgradeRequest request, ClaimsPrincipal user, RegistryDbContext db, StripeBilling stripe, CancellationToken ct) =>
        {
            if (await AccountEndpoints.MemberAsync(db, user, ct) is not { } member)
            {
                return Results.Problem("Create your company first.", statusCode: 404);
            }
            var tier = request.Tier ?? "";
            var interval = request.Interval ?? "month";
            var currency = request.Currency ?? "eur";
            if (!Plans.SelfServe.Contains(tier))
            {
                return Results.Problem("That plan is arranged with us directly: write to hello@signunder.com.", statusCode: 400);
            }
            if (!Plans.Intervals.Contains(interval) || !Plans.Currencies.Contains(currency))
            {
                return Results.Problem("Choose monthly or yearly, in euro or dollars.", statusCode: 400);
            }
            var tenant = await db.Tenants.SingleAsync(t => t.Id == member.TenantId, ct);
            if (tenant.Tier == tier)
            {
                return Results.Problem($"You are already on {tier}.", statusCode: 409);
            }
            var billing = await db.TenantBillings.SingleOrDefaultAsync(b => b.TenantId == tenant.Id, ct);

            if (stripe.Enabled)
            {
                if (stripe.PriceId(tier, interval, currency) is not { } price)
                {
                    return Results.Problem("Card payment for that choice is not set up yet; write to hello@signunder.com.", statusCode: 503);
                }
                try
                {
                    return Results.Json(new JsonObject { ["checkout_url"] = await stripe.CheckoutAsync(tenant, billing, member.Email, tier, price, ct) });
                }
                catch (Exception e) when (e is StripeException or HttpRequestException or TaskCanceledException)
                {
                    return Results.Problem("Card payment is unavailable just now; please try again shortly.", statusCode: 502);
                }
            }

            // By invoice: record what was asked for; the daily billing issue lists it.
            if (billing is null)
            {
                billing = new TenantBilling { TenantId = tenant.Id };
                db.TenantBillings.Add(billing);
            }
            billing.RequestedTier = tier;
            billing.RequestedInterval = interval;
            billing.RequestedCurrency = currency;
            billing.RequestedBy = member.Email;
            billing.RequestedAt = DateTimeOffset.UtcNow;
            billing.UpdatedAt = billing.RequestedAt.Value;
            await db.SaveChangesAsync(ct);
            return Results.Json(View(tenant, billing, stripe), statusCode: 202);
        });

        account.MapPost("/billing/portal", async (ClaimsPrincipal user, RegistryDbContext db, StripeBilling stripe, CancellationToken ct) =>
        {
            var member = await AccountEndpoints.MemberAsync(db, user, ct);
            var billing = member is null ? null : await db.TenantBillings.AsNoTracking().SingleOrDefaultAsync(b => b.TenantId == member.TenantId, ct);
            if (!stripe.Enabled || billing?.StripeCustomerId is not { } customer)
            {
                return Results.Problem("There is no card subscription to manage; for invoices, write to hello@signunder.com.", statusCode: 404);
            }
            try
            {
                return Results.Json(new JsonObject { ["url"] = await stripe.PortalAsync(customer, ct) });
            }
            catch (Exception e) when (e is StripeException or HttpRequestException or TaskCanceledException)
            {
                return Results.Problem("Billing management is unavailable just now; please try again shortly.", statusCode: 502);
            }
        });
    }

    /// <summary>
    /// Stripe's events, signed with <c>Billing:Stripe:WebhookSecret</c>. Each sets the
    /// state it describes, so a repeat delivery changes nothing.
    /// </summary>
    public static void MapStripeWebhook(this IEndpointRouteBuilder app)
    {
        app.MapPost("/webhooks/stripe", async (HttpRequest http, RegistryDbContext db, StripeBilling stripe, IConfiguration config, TimeProvider time, ILoggerFactory logs, CancellationToken ct) =>
        {
            if (!stripe.Enabled)
            {
                return Results.NotFound();
            }
            using var reader = new StreamReader(http.Body);
            var payload = await reader.ReadToEndAsync(ct);
            if (!StripeBilling.SignatureValid(payload, http.Headers["Stripe-Signature"], config["Billing:Stripe:WebhookSecret"] ?? "", time.GetUtcNow()))
            {
                return Results.Problem("Bad signature.", statusCode: 400);
            }

            using var json = JsonDocument.Parse(payload);
            var type = json.RootElement.GetProperty("type").GetString();
            var data = json.RootElement.GetProperty("data").GetProperty("object");
            var log = logs.CreateLogger("Signunder.Billing");

            switch (type)
            {
                case "checkout.session.completed":
                    if (Guid.TryParse(Text(data, "client_reference_id"), out var tenantId)
                        && await db.Tenants.SingleOrDefaultAsync(t => t.Id == tenantId, ct) is { } tenant)
                    {
                        var billing = await BillingFor(db, tenantId, ct);
                        billing.StripeCustomerId = Text(data, "customer") ?? billing.StripeCustomerId;
                        billing.StripeSubscriptionId = Text(data, "subscription") ?? billing.StripeSubscriptionId;
                        billing.SubscriptionStatus ??= "active";
                        ClearRequest(billing);
                        tenant.Tier = Metadata(data, "tier") is { } tier && Plans.SelfServe.Contains(tier) ? tier : Plans.Growth;
                        billing.UpdatedAt = time.GetUtcNow();
                        await db.SaveChangesAsync(ct);
                        log.LogInformation("Tenant {Tenant} subscribed to {Tier}", tenantId, tenant.Tier);
                    }
                    break;

                case "customer.subscription.created" or "customer.subscription.updated" or "customer.subscription.deleted":
                    var subscription = Text(data, "id");
                    var billingRow = await db.TenantBillings.SingleOrDefaultAsync(b => b.StripeSubscriptionId == subscription, ct);
                    if (billingRow is null && Guid.TryParse(Metadata(data, "tenant_id"), out var byMetadata))
                    {
                        billingRow = await BillingFor(db, byMetadata, ct);
                        billingRow.StripeSubscriptionId = subscription;
                        billingRow.StripeCustomerId ??= Text(data, "customer");
                    }
                    if (billingRow is not null && await db.Tenants.SingleOrDefaultAsync(t => t.Id == billingRow.TenantId, ct) is { } owner)
                    {
                        var status = type == "customer.subscription.deleted" ? "canceled" : Text(data, "status") ?? "active";
                        billingRow.SubscriptionStatus = status;
                        owner.Tier = Plans.PaidStatuses.Contains(status)
                            ? (Metadata(data, "tier") is { } tier && Plans.SelfServe.Contains(tier) ? tier : owner.Tier == Plans.Free ? Plans.Growth : owner.Tier)
                            : Plans.Free;
                        billingRow.UpdatedAt = time.GetUtcNow();
                        await db.SaveChangesAsync(ct);
                        log.LogInformation("Tenant {Tenant} subscription {Status}: plan {Tier}", owner.Id, status, owner.Tier);
                    }
                    break;
            }
            return Results.Ok();
        }).DisableAntiforgery().WithMetadata(new Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute(1_000_000));
    }

    private static async Task<TenantBilling> BillingFor(RegistryDbContext db, Guid tenantId, CancellationToken ct)
    {
        if (await db.TenantBillings.SingleOrDefaultAsync(b => b.TenantId == tenantId, ct) is { } existing)
        {
            return existing;
        }
        var created = new TenantBilling { TenantId = tenantId };
        db.TenantBillings.Add(created);
        return created;
    }

    private static void ClearRequest(TenantBilling billing)
    {
        billing.RequestedTier = null;
        billing.RequestedInterval = null;
        billing.RequestedCurrency = null;
        billing.RequestedBy = null;
        billing.RequestedAt = null;
    }

    private static string? Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static string? Metadata(JsonElement element, string name) =>
        element.TryGetProperty("metadata", out var metadata) && metadata.ValueKind == JsonValueKind.Object ? Text(metadata, name) : null;
}
