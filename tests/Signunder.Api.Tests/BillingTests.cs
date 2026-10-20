using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Signunder.Api.Account;
using Signunder.Api.Data;
using Signunder.Api.Signing;

namespace Signunder.Api.Tests;

/// <summary>Upgrades while billing is by invoice: the default, until there is a Stripe account.</summary>
public class BillingByInvoiceTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task An_upgrade_is_recorded_as_a_request_and_listed_for_invoicing()
    {
        var (client, slug) = await BillingTestSupport.CompanyAsync(api, "Invoice Co");

        var answer = await client.PostAsJsonAsync("/v1/account/upgrade", new { tier = "growth", interval = "year", currency = "usd" });

        Assert.Equal(HttpStatusCode.Accepted, answer.StatusCode);
        var account = await client.GetFromJsonAsync<JsonObject>("/v1/account");
        var billing = account!["billing"]!;
        Assert.Equal("free", (string?)billing["plan"]);
        Assert.Equal("invoice", (string?)billing["payment"]);
        Assert.Equal("growth", (string?)billing["requested"]!["tier"]);
        Assert.Equal("year", (string?)billing["requested"]!["interval"]);
        Assert.Equal("usd", (string?)billing["requested"]!["currency"]);
        Assert.False((bool)billing["can_manage"]!);
        Assert.Contains(slug, await BillingTestSupport.PendingAsync(api));
    }

    [Fact]
    public async Task Only_self_serve_plans_and_known_choices_are_accepted()
    {
        var (client, _) = await BillingTestSupport.CompanyAsync(api, "Choosy Co");

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/v1/account/upgrade", new { tier = "business" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/v1/account/upgrade", new { tier = "growth", currency = "gbp" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/v1/account/billing/portal", null)).StatusCode);
    }

    [Fact]
    public async Task The_stripe_webhook_is_off_without_stripe()
    {
        var response = await api.CreateClient().PostAsync("/webhooks/stripe", new StringContent("{}"));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void A_plan_with_overage_keeps_signing_and_counts_the_extra()
    {
        var start = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var growth = new QuotaState("growth", 2, 5, start, start.AddMonths(1), OverageAllowed: true);
        Assert.False(growth.Exhausted);
        Assert.Equal(3, growth.Overage);

        var free = new QuotaState("free", 2, 5, start, start.AddMonths(1));
        Assert.True(free.Exhausted);
    }
}

/// <summary>Hosts the API with card payment on, Stripe answered by the fake upstream.</summary>
public sealed class StripeApiFactory : ApiFactory
{
    public const string StripeBase = "https://stripe.signunder.example/";
    public const string WebhookSecret = "whsec_test_secret";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Billing:Provider", "stripe");
        builder.UseSetting("Billing:Stripe:SecretKey", "sk_test_fake");
        builder.UseSetting("Billing:Stripe:WebhookSecret", WebhookSecret);
        builder.UseSetting("Billing:Stripe:ApiBase", StripeBase);
        builder.UseSetting("Billing:ReturnUrl", "https://signunder.example/dashboard");
        builder.UseSetting("Billing:Stripe:Prices:growth:month:eur", "price_growth_month_eur");
        builder.UseSetting("Billing:Stripe:Prices:growth:year:usd", "price_growth_year_usd");
        builder.ConfigureTestServices(services =>
            services.AddHttpClient(StripeBilling.Client).ConfigurePrimaryHttpMessageHandler(() => Upstream));
    }
}

public class BillingByCardTests(StripeApiFactory api) : IClassFixture<StripeApiFactory>
{
    [Fact]
    public async Task An_upgrade_goes_to_stripe_checkout_for_the_chosen_price()
    {
        var (client, _) = await BillingTestSupport.CompanyAsync(api, "Card Co");
        string? sent = null;
        api.Upstream.On("POST", StripeApiFactory.StripeBase + "v1/checkout/sessions", request =>
        {
            sent = request.Content!.ReadAsStringAsync().Result;
            return FakeUpstream.Json("""{"id":"cs_test_1","url":"https://checkout.stripe.example/c/cs_test_1"}""");
        });

        var answer = await client.PostAsJsonAsync("/v1/account/upgrade", new { tier = "growth", interval = "year", currency = "usd" });

        Assert.Equal(HttpStatusCode.OK, answer.StatusCode);
        Assert.Equal("https://checkout.stripe.example/c/cs_test_1", (string?)(await answer.Content.ReadFromJsonAsync<JsonObject>())!["checkout_url"]);
        var form = Uri.UnescapeDataString(sent!);
        Assert.Contains("mode=subscription", form);
        Assert.Contains("line_items[0][price]=price_growth_year_usd", form);
        Assert.Contains("client_reference_id=", form);
        Assert.Contains("allow_promotion_codes=true", form);

        // A choice with no price configured is refused, not guessed.
        Assert.Equal(HttpStatusCode.ServiceUnavailable,
            (await client.PostAsJsonAsync("/v1/account/upgrade", new { tier = "growth", interval = "month", currency = "usd" })).StatusCode);
    }

    [Fact]
    public async Task Stripe_events_set_the_plan_and_unsigned_ones_are_refused()
    {
        var (client, slug) = await BillingTestSupport.CompanyAsync(api, "Webhook Co");
        var tenantId = await BillingTestSupport.TenantIdAsync(api, slug);
        var subscription = $"sub_{Guid.NewGuid():N}";

        var completed = $$$$$"""{"id":"evt_1","type":"checkout.session.completed","data":{"object":{"id":"cs_1","client_reference_id":"{{{{{tenantId}}}}}","customer":"cus_1","subscription":"{{{{{subscription}}}}}","metadata":{"tier":"growth"}}}}""";
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(completed, "whsec_wrong")).StatusCode);
        Assert.Equal("free", await PlanAsync(client));

        Assert.Equal(HttpStatusCode.OK, (await Post(completed, StripeApiFactory.WebhookSecret)).StatusCode);
        Assert.Equal("growth", await PlanAsync(client));
        var account = await client.GetFromJsonAsync<JsonObject>("/v1/account");
        Assert.True((bool)account!["billing"]!["can_manage"]!);

        // A failed renewal keeps the plan while Stripe retries; cancellation ends it.
        await Post($$$$$"""{"id":"evt_2","type":"customer.subscription.updated","data":{"object":{"id":"{{{{{subscription}}}}}","status":"past_due","metadata":{"tier":"growth"}}}}""", StripeApiFactory.WebhookSecret);
        Assert.Equal("growth", await PlanAsync(client));
        await Post($$$$$"""{"id":"evt_3","type":"customer.subscription.deleted","data":{"object":{"id":"{{{{{subscription}}}}}","status":"canceled"}}}""", StripeApiFactory.WebhookSecret);
        Assert.Equal("free", await PlanAsync(client));
    }

    [Fact]
    public async Task A_subscriber_manages_billing_in_the_stripe_portal()
    {
        var (client, slug) = await BillingTestSupport.CompanyAsync(api, "Portal Co");
        var tenantId = await BillingTestSupport.TenantIdAsync(api, slug);
        await Post($$$$$"""{"id":"evt_p","type":"checkout.session.completed","data":{"object":{"client_reference_id":"{{{{{tenantId}}}}}","customer":"cus_portal","subscription":"sub_{{{{{Guid.NewGuid():N}}}}}"}}}""", StripeApiFactory.WebhookSecret);
        api.Upstream.OnJson("POST", StripeApiFactory.StripeBase + "v1/billing_portal/sessions", """{"url":"https://billing.stripe.example/p/1"}""");

        var portal = await client.PostAsync("/v1/account/billing/portal", null);

        Assert.Equal("https://billing.stripe.example/p/1", (string?)(await portal.Content.ReadFromJsonAsync<JsonObject>())!["url"]);
    }

    [Fact]
    public void Signatures_follow_stripes_scheme()
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
        var payload = """{"type":"x"}""";
        var good = BillingTestSupport.Sign(payload, StripeApiFactory.WebhookSecret, now);

        Assert.True(StripeBilling.SignatureValid(payload, good, StripeApiFactory.WebhookSecret, now));
        // A second v1 (a rolled secret) and a v0 are both allowed in the header.
        Assert.True(StripeBilling.SignatureValid(payload, $"{good},v1=00,v0=11", StripeApiFactory.WebhookSecret, now));
        Assert.False(StripeBilling.SignatureValid(payload + " ", good, StripeApiFactory.WebhookSecret, now));
        Assert.False(StripeBilling.SignatureValid(payload, good, StripeApiFactory.WebhookSecret, now.AddMinutes(6)));
        Assert.False(StripeBilling.SignatureValid(payload, good.Replace("v1=", "v0="), StripeApiFactory.WebhookSecret, now));
        Assert.False(StripeBilling.SignatureValid(payload, null, StripeApiFactory.WebhookSecret, now));
    }

    private Task<HttpResponseMessage> Post(string payload, string secret)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/stripe") { Content = new StringContent(payload, Encoding.UTF8, "application/json") };
        request.Headers.Add("Stripe-Signature", BillingTestSupport.Sign(payload, secret, DateTimeOffset.UtcNow));
        return api.CreateClient().SendAsync(request);
    }

    private static async Task<string?> PlanAsync(HttpClient client) =>
        (string?)(await client.GetFromJsonAsync<JsonObject>("/v1/account"))!["billing"]!["plan"];
}

internal static class BillingTestSupport
{
    public static async Task<(HttpClient Client, string Slug)> CompanyAsync(ApiFactory api, string name)
    {
        var client = api.ClientWithUser(ApiFactory.UserToken(Guid.NewGuid(), $"{Guid.NewGuid():N}@example.com"));
        var created = await (await client.PostAsJsonAsync("/v1/account/tenant", new { name })).Content.ReadFromJsonAsync<JsonObject>();
        return (client, (string)created!["tenant"]!["slug"]!);
    }

    public static async Task<Guid> TenantIdAsync(ApiFactory api, string slug)
    {
        await using var scope = api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RegistryDbContext>();
        return await db.Tenants.Where(t => t.Slug == slug).Select(t => t.Id).SingleAsync();
    }

    /// <summary>The slugs in the view the billing-requests workflow reads.</summary>
    public static async Task<List<string>> PendingAsync(ApiFactory api)
    {
        await using var scope = api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RegistryDbContext>();
        return await db.Database.SqlQueryRaw<string>("SELECT slug AS \"Value\" FROM registry.upgrade_requests_pending").ToListAsync();
    }

    public static string Sign(string payload, string secret, DateTimeOffset at)
    {
        var t = at.ToUnixTimeSeconds();
        var signature = Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{t}.{payload}")));
        return $"t={t},v1={signature}";
    }
}
