using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Signunder.Api.Data;

namespace Signunder.Api.Tests;

public class IdentityTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static async Task<JsonObject> Json(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonObject>())!;

    /// <summary>A signed-in person with a company; returns their session token, API key and slug.</summary>
    private async Task<(string Token, string Key, string Slug)> CompanyAsync(string name)
    {
        var token = ApiFactory.UserToken(Guid.NewGuid(), $"{Guid.NewGuid():N}@example.com");
        var created = await Json(await api.ClientWithUser(token).PostAsJsonAsync("/v1/account/tenant", new { name }));
        return (token, (string)created["api_key"]!["key"]!, (string)created["tenant"]!["slug"]!);
    }

    private void DnsAnswers(string domain, params string[] txt) =>
        api.Upstream.OnJson("GET", $"{ApiFactory.FakeDns}?name={domain}&type=TXT", new JsonObject
        {
            ["Status"] = 0,
            ["Answer"] = new JsonArray([.. txt.Select(t => (JsonNode)new JsonObject { ["name"] = domain, ["type"] = 16, ["data"] = $"\"{t}\"" })]),
        }.ToJsonString());

    private async Task ApproveAsync(string slug, string name)
    {
        await using var scope = api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RegistryDbContext>();
        var tenant = await db.Tenants.SingleAsync(t => t.Slug == slug);
        var identity = await db.TenantIdentities.SingleAsync(i => i.TenantId == tenant.Id);
        identity.ApprovedName = name;
        identity.ApprovedAt = DateTimeOffset.UtcNow;
        identity.CertChainPem = "-----BEGIN CERTIFICATE-----\nidentity\n-----END CERTIFICATE-----\n";
        identity.KeyRef = "dev:local";
        await db.SaveChangesAsync();
    }

    private async Task<string> SignAsync(string key)
    {
        var file = new ByteArrayContent([40, (byte)Random.Shared.Next(255)]);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        var response = await api.ClientWithKey(key).PostAsync("/v1/sign", new MultipartFormDataContent { { file, "file", "a.jpg" } });
        response.EnsureSuccessStatusCode();
        return response.Headers.GetValues("SignUnder-Record-Id").Single();
    }

    [Fact]
    public async Task A_company_names_its_domain_and_gets_the_txt_record_to_publish()
    {
        var (token, _, _) = await CompanyAsync("Acme Images Ltd");
        var client = api.ClientWithUser(token);

        var set = await Json(await client.PostAsJsonAsync("/v1/account/domain", new { domain = "https://www.Acme-Images.example/about" }));

        Assert.Equal("acme-images.example", (string?)set["domain"]);
        Assert.Equal("unverified", (string?)set["status"]);
        Assert.StartsWith("signunder-verification=", (string?)set["txt_record"]!["value"]);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/v1/account/domain", new { domain = "not a domain" })).StatusCode);
    }

    [Fact]
    public async Task The_domain_is_verified_only_when_its_txt_record_is_published()
    {
        var (token, _, _) = await CompanyAsync("Verify Co");
        var client = api.ClientWithUser(token);
        var set = await Json(await client.PostAsJsonAsync("/v1/account/domain", new { domain = "verify-co.example" }));
        var txt = (string)set["txt_record"]!["value"]!;

        DnsAnswers("verify-co.example", "v=spf1 -all");
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync("/v1/account/domain/check", null)).StatusCode);

        DnsAnswers("verify-co.example", "v=spf1 -all", txt);
        var checkedNow = await Json(await client.PostAsync("/v1/account/domain/check", null));
        Assert.Equal("awaiting_review", (string?)checkedNow["status"]);

        // Changing the domain starts again.
        var changed = await Json(await client.PostAsJsonAsync("/v1/account/domain", new { domain = "other.example" }));
        Assert.Equal("unverified", (string?)changed["status"]);
        Assert.NotEqual(txt, (string?)changed["txt_record"]!["value"]);
    }

    [Fact]
    public async Task Signatures_name_the_company_only_once_its_name_is_approved()
    {
        var (token, key, slug) = await CompanyAsync("Named Co");
        var client = api.ClientWithUser(token);
        var set = await Json(await client.PostAsJsonAsync("/v1/account/domain", new { domain = "named-co.example" }));
        DnsAnswers("named-co.example", (string)set["txt_record"]!["value"]!);
        await client.PostAsync("/v1/account/domain/check", null);

        // Verified but not approved: no identity.
        var unnamed = await SignAsync(key);
        Assert.Null(api.Sidecar.LastRequest!.Identity);
        Assert.Null((await api.ClientWithKey(null).GetFromJsonAsync<JsonObject>($"/public/v1/{slug}/records/{unnamed}"))!["published_by"]);

        await ApproveAsync(slug, "Named Co");
        var named = await SignAsync(key);

        var identity = api.Sidecar.LastRequest!.Identity;
        Assert.NotNull(identity);
        Assert.Equal("dev:local", identity.KeyRef);
        Assert.Contains("cawg.publisher", identity.Roles);
        var record = await api.ClientWithKey(null).GetFromJsonAsync<JsonObject>($"/public/v1/{slug}/records/{named}");
        Assert.Equal("Named Co", (string?)record!["published_by"]!["name"]);
        Assert.Equal("named-co.example", (string?)record["published_by"]!["domain"]);
        Assert.Equal("SignUnder", (string?)record["published_by"]!["verified_by"]);
        Assert.Equal("named-co.example", (string?)(await api.ClientWithKey(null).GetFromJsonAsync<JsonObject>($"/public/v1/{slug}"))!["verified_domain"]);
    }

    [Fact]
    public async Task Renaming_the_company_stops_naming_it_until_approved_again()
    {
        var (token, key, slug) = await CompanyAsync("Before Rename");
        var client = api.ClientWithUser(token);
        var set = await Json(await client.PostAsJsonAsync("/v1/account/domain", new { domain = "rename.example" }));
        DnsAnswers("rename.example", (string)set["txt_record"]!["value"]!);
        await client.PostAsync("/v1/account/domain/check", null);
        await ApproveAsync(slug, "A Different Name");

        await SignAsync(key);

        Assert.Null(api.Sidecar.LastRequest!.Identity);
    }
}
