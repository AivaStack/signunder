using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;

namespace Signunder.Api.Tests;

/// <summary>Findings of the OWASP Top 10 review (14 Oct 2026).</summary>
public class HardeningTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static MultipartFormDataContent Image(byte[] bytes)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        return new MultipartFormDataContent { { file, "file", "picture.jpg" } };
    }

    [Fact]
    public async Task Responses_carry_the_security_headers()
    {
        var health = await api.ClientWithKey(null).GetAsync("/healthz");
        Assert.Equal("nosniff", health.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("same-origin", health.Headers.GetValues("Cross-Origin-Resource-Policy").Single());
        Assert.False(health.Headers.CacheControl?.NoStore ?? false);
        Assert.False(health.Headers.Contains("Strict-Transport-Security"));

        // Over HTTPS, browsers are told to stay on HTTPS.
        var https = api.ClientWithKey(null);
        https.BaseAddress = new Uri("https://localhost");
        var secure = await https.GetAsync("/healthz");
        Assert.Equal("max-age=31536000; includeSubDomains", secure.Headers.GetValues("Strict-Transport-Security").Single());

        // Signed copies are meant to be embedded by customers' pages elsewhere.
        var media = await api.ClientWithKey(null).GetAsync("/media/01ARZ3NDEKTSV4RRFFQ69G5FAV");
        Assert.Equal("cross-origin", media.Headers.GetValues("Cross-Origin-Resource-Policy").Single());

        // Tenant data is not cached.
        var record = await api.ClientWithKey(ApiFactory.DemoKey).GetAsync("/v1/records/01ARZ3NDEKTSV4RRFFQ69G5FAV");
        Assert.True(record.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task Internal_failures_are_reported_by_reference_without_details()
    {
        var response = await api.ClientWithKey(ApiFactory.DemoKey).PostAsync("/v1/sign", Image(FakeSidecar.FailingAsset));

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Quote reference", body);
        Assert.DoesNotContain("secret-key-id", body);
        Assert.DoesNotContain("KMS", body);
        Assert.DoesNotContain("sidecar", body, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class TightRateLimitApiFactory : ApiFactory
{
    protected override int RateLimitPerMinute => 3;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("ForwardedHeaders:ClientIpHeader", "Fly-Client-IP");
    }
}

public class RateLimitTests(TightRateLimitApiFactory api) : IClassFixture<TightRateLimitApiFactory>
{
    [Fact]
    public async Task Each_api_key_has_its_own_limit()
    {
        var first = api.ClientWithKey(ApiFactory.DemoKey);
        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(HttpStatusCode.NotFound, (await first.GetAsync("/v1/records/01ARZ3NDEKTSV4RRFFQ69G5FAV")).StatusCode);
        }

        var limited = await first.GetAsync("/v1/records/01ARZ3NDEKTSV4RRFFQ69G5FAV");
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Equal("60", limited.Headers.GetValues("Retry-After").Single());

        // Another tenant's key is unaffected.
        var other = api.ClientWithKey(await api.AddTenantAsync("rate-other", Core.Registry.ApiScopes.All, "arn:aws:kms:eu-central-1:1:key/o"));
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync("/v1/records/01ARZ3NDEKTSV4RRFFQ69G5FAV")).StatusCode);
    }

    [Fact]
    public async Task Callers_without_a_key_are_limited_by_the_address_the_proxy_reports()
    {
        // Behind Fly.io every connection comes from the proxy; Fly-Client-IP names the caller.
        async Task<HttpStatusCode> Media(string address)
        {
            var client = api.ClientWithKey(null);
            client.DefaultRequestHeaders.Add("Fly-Client-IP", address);
            return (await client.GetAsync("/media/01ARZ3NDEKTSV4RRFFQ69G5FAV")).StatusCode;
        }

        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(HttpStatusCode.NotFound, await Media("203.0.113.1"));
        }
        Assert.Equal(HttpStatusCode.TooManyRequests, await Media("203.0.113.1"));

        // A different caller through the same proxy has its own limit.
        Assert.Equal(HttpStatusCode.NotFound, await Media("203.0.113.2"));
    }
}
