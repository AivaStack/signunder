using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;

namespace Signunder.Api.Tests;

/// <summary>A host whose free plan allows two signed outputs a month.</summary>
public sealed class TightQuotaApiFactory : ApiFactory
{
    public const int FreeLimit = 2;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Quotas:MonthlyOutputs:free", FreeLimit.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
}

public class QuotaTests(TightQuotaApiFactory api) : IClassFixture<TightQuotaApiFactory>
{
    private static MultipartFormDataContent Image(byte b)
    {
        var file = new ByteArrayContent([30, b]);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        return new MultipartFormDataContent { { file, "file", "a.jpg" } };
    }

    private async Task<string> FreeTenantAsync(string slug) =>
        await api.AddTenantAsync(slug, Core.Registry.ApiScopes.All, "dev:local");

    private async Task UseUpAsync(string key)
    {
        var client = api.ClientWithKey(key);
        for (var i = 0; i < TightQuotaApiFactory.FreeLimit; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/v1/sign", Image((byte)i))).StatusCode);
        }
    }

    [Fact]
    public async Task Signing_stops_at_the_monthly_allowance_for_that_tenant_only()
    {
        var key = await FreeTenantAsync("quota-full");
        await UseUpAsync(key);

        var refused = await api.ClientWithKey(key).PostAsync("/v1/sign", Image(9));

        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        var detail = (string?)(await refused.Content.ReadFromJsonAsync<JsonObject>())!["detail"];
        var next = DateTime.UtcNow.AddMonths(1);
        Assert.Contains($"resets on 1 {next:MMMM yyyy}", detail);

        var other = await FreeTenantAsync("quota-other");
        Assert.Equal(HttpStatusCode.OK, (await api.ClientWithKey(other).PostAsync("/v1/sign", Image(1))).StatusCode);
    }

    [Fact]
    public async Task Proxy_mode_refuses_a_new_generation_before_the_provider_makes_it()
    {
        var key = await FreeTenantAsync("quota-proxy");
        await UseUpAsync(key);
        var client = api.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Key fal-customer-key");
        client.DefaultRequestHeaders.Add("X-SignUnder-Key", key);
        var before = api.Upstream.Requests.Count;

        var submit = await client.PostAsync(
            "/proxy/fal/queue/fal-ai/flux/schnell", new StringContent("""{"prompt":"x"}""", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.TooManyRequests, submit.StatusCode);
        Assert.Equal(before, api.Upstream.Requests.Count);
    }

    [Fact]
    public async Task The_dashboard_shows_the_month_s_usage()
    {
        var token = ApiFactory.UserToken(Guid.NewGuid(), "usage@acme.example");
        var created = await (await api.ClientWithUser(token).PostAsJsonAsync("/v1/account/tenant", new { name = "Usage Co" }))
            .Content.ReadFromJsonAsync<JsonObject>();
        var key = (string)created!["api_key"]!["key"]!;
        Assert.Equal(HttpStatusCode.OK, (await api.ClientWithKey(key).PostAsync("/v1/sign", Image(1))).StatusCode);

        var usage = (await api.ClientWithUser(token).GetFromJsonAsync<JsonObject>("/v1/account"))!["usage"]!;

        Assert.Equal(1, (int?)usage["signed"]);
        Assert.Equal(TightQuotaApiFactory.FreeLimit, (int?)usage["limit"]);
    }
}
