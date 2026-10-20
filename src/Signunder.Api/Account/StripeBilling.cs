using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Signunder.Core.Registry;

namespace Signunder.Api.Account;

/// <summary>
/// Card payment through Stripe (ADR 0015), over its REST API with no SDK: Checkout for a
/// new subscription, the customer portal for changing or cancelling it, and webhook
/// signatures. On only when <c>Billing:Provider</c> is <c>stripe</c>; until then upgrades are
/// requests that Eoin invoices by hand.
/// </summary>
public sealed class StripeBilling(IHttpClientFactory clients, IConfiguration config)
{
    public const string Client = "stripe";

    /// <summary>Stripe's own libraries allow five minutes between signing and receipt.</summary>
    public static readonly TimeSpan SignatureTolerance = TimeSpan.FromMinutes(5);

    public bool Enabled => config["Billing:Provider"] == "stripe" && !string.IsNullOrEmpty(config["Billing:Stripe:SecretKey"]);

    /// <summary>Where the dashboard is, for Stripe to send people back to.</summary>
    public string ReturnUrl => config["Billing:ReturnUrl"] ?? "https://signunder.com/dashboard";

    /// <summary>The Stripe price for a plan, interval and currency: <c>Billing:Stripe:Prices:{tier}:{interval}:{currency}</c>.</summary>
    public string? PriceId(string tier, string interval, string currency) =>
        config[$"Billing:Stripe:Prices:{tier}:{interval}:{currency}"];

    /// <summary>Starts a Checkout session for a subscription and returns the page to send the buyer to.</summary>
    public async Task<string> CheckoutAsync(
        Tenant tenant, TenantBilling? billing, string email, string tier, string price, CancellationToken ct)
    {
        var form = new List<KeyValuePair<string, string>>
        {
            new("mode", "subscription"),
            new("line_items[0][price]", price),
            new("line_items[0][quantity]", "1"),
            new("success_url", ReturnUrl + "?billing=done"),
            new("cancel_url", ReturnUrl + "?billing=cancelled"),
            // How the webhook finds the tenant again.
            new("client_reference_id", tenant.Id.ToString()),
            new("metadata[tenant_id]", tenant.Id.ToString()),
            new("metadata[tier]", tier),
            new("subscription_data[metadata][tenant_id]", tenant.Id.ToString()),
            new("subscription_data[metadata][tier]", tier),
            // The founding-customer price is a promotion code.
            new("allow_promotion_codes", "true"),
        };
        form.Add(billing?.StripeCustomerId is { } customer
            ? new("customer", customer)
            : new("customer_email", email));
        using var response = await SendAsync("v1/checkout/sessions", form, ct);
        return await UrlAsync(response, ct);
    }

    /// <summary>A customer-portal session, where the buyer changes card or plan, or cancels.</summary>
    public async Task<string> PortalAsync(string customer, CancellationToken ct)
    {
        using var response = await SendAsync("v1/billing_portal/sessions", [new("customer", customer), new("return_url", ReturnUrl)], ct);
        return await UrlAsync(response, ct);
    }

    private async Task<HttpResponseMessage> SendAsync(string path, IEnumerable<KeyValuePair<string, string>> form, CancellationToken ct)
    {
        var apiBase = config["Billing:Stripe:ApiBase"] ?? "https://api.stripe.com/";
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(apiBase), path))
        {
            Content = new FormUrlEncodedContent(form),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config["Billing:Stripe:SecretKey"]);
        var response = await clients.CreateClient(Client).SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            response.Dispose();
            throw new StripeException($"Stripe answered {(int)response.StatusCode} to {path}: {body[..Math.Min(body.Length, 300)]}");
        }
        return response;
    }

    private static async Task<string> UrlAsync(HttpResponseMessage response, CancellationToken ct)
    {
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return body.RootElement.TryGetProperty("url", out var url) && url.GetString() is { } value
            ? value
            : throw new StripeException("Stripe's answer had no url.");
    }

    /// <summary>
    /// Whether <paramref name="header"/> (<c>Stripe-Signature: t=…,v1=…</c>) signs
    /// <paramref name="payload"/> with <paramref name="secret"/>: HMAC-SHA256 of
    /// <c>{t}.{payload}</c>, compared in constant time, with <c>t</c> within the tolerance.
    /// Schemes other than <c>v1</c> are ignored, as Stripe asks.
    /// </summary>
    public static bool SignatureValid(string payload, string? header, string secret, DateTimeOffset now)
    {
        if (string.IsNullOrEmpty(header) || string.IsNullOrEmpty(secret))
        {
            return false;
        }
        long? timestamp = null;
        var signatures = new List<string>();
        foreach (var part in header.Split(','))
        {
            var (key, value) = part.IndexOf('=') is var i and > 0 ? (part[..i].Trim(), part[(i + 1)..].Trim()) : ("", "");
            if (key == "t" && long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var t))
            {
                timestamp = t;
            }
            else if (key == "v1")
            {
                signatures.Add(value);
            }
        }
        if (timestamp is not { } signedAt || signatures.Count == 0
            || (now - DateTimeOffset.FromUnixTimeSeconds(signedAt)).Duration() > SignatureTolerance)
        {
            return false;
        }
        var expected = Encoding.ASCII.GetBytes(Convert.ToHexStringLower(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{signedAt}.{payload}"))));
        return signatures.Any(s => CryptographicOperations.FixedTimeEquals(expected, Encoding.ASCII.GetBytes(s)));
    }
}

public sealed class StripeException(string message) : Exception(message);
