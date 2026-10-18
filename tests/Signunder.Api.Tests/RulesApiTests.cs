using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Signunder.Api.Tests;

public class RulesApiTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private async Task<HttpResponseMessage> EvaluateAsync(object answers) =>
        await api.ClientWithKey(null).PostAsJsonAsync("/public/v1/rules/evaluate", answers);

    [Fact]
    public async Task The_packs_are_public_with_their_versions_and_sources()
    {
        var body = await api.ClientWithKey(null).GetFromJsonAsync<JsonObject>("/public/v1/rules/packs");

        var packs = body!["packs"]!.AsArray();
        Assert.Equal(4, packs.Count);
        var eu = packs.Single(p => (string?)p!["pack"] == "eu-ai-act-art50")!;
        Assert.Equal("2026.10.12", (string?)eu["version"]);
        Assert.Equal("2026-10-12", (string?)eu["sources"]![0]!["read"]);
        Assert.NotEmpty(eu["rules"]![0]!["source_ids"]!.AsArray());
    }

    [Fact]
    public async Task An_evaluation_on_a_date_is_reproducible_and_names_its_packs()
    {
        var response = await EvaluateAsync(new
        {
            modalities = new[] { "image" },
            roles = new[] { "provider" },
            markets = new[] { "eu" },
            on_market_before_2026_08_02 = true,
            date = "2026-10-18",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
        Assert.Equal("2026-10-18", (string?)body["date"]);
        Assert.Equal(4, body["pack_versions"]!.AsArray().Count);
        Assert.Equal(["machine_readable_marking"], body["required"]!.AsArray().Select(r => (string)r!));
        var marking = body["findings"]!.AsArray().Single(f => (string?)f!["rule_id"] == "eu-50-2-marking")!;
        Assert.Equal("upcoming", (string?)marking["status"]);
        Assert.Equal("2026-12-02", (string?)marking["applies_from"]);
        Assert.Equal("/law/eu-ai-act-article-50", (string?)marking["page"]);
        Assert.NotEmpty(marking["sources"]!.AsArray());
    }

    [Fact]
    public async Task Without_a_date_it_evaluates_today()
    {
        var body = await (await EvaluateAsync(new { modalities = new[] { "video" }, markets = new[] { "eu" }, roles = new[] { "deployer" }, realistic = true }))
            .Content.ReadFromJsonAsync<JsonObject>();

        Assert.Equal(DateTime.UtcNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), (string?)body!["date"]);
        Assert.Contains(body["findings"]!.AsArray(), f => (string?)f!["rule_id"] == "eu-50-4-deepfake");
    }

    [Theory]
    [InlineData("""{"modalities":["image"],"markets":["mars"]}""", "markets")]
    [InlineData("""{"modalities":[],"markets":["eu"]}""", "modalities")]
    [InlineData("""{"modalities":["hologram"]}""", "modalities")]
    public async Task Unknown_or_missing_answers_are_refused_with_the_known_values(string json, string field)
    {
        var response = await api.ClientWithKey(null).PostAsync(
            "/public/v1/rules/evaluate", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(field, (string?)(await response.Content.ReadFromJsonAsync<JsonObject>())!["detail"]);
    }

    [Fact]
    public async Task The_website_may_call_it()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/public/v1/rules/evaluate");
        request.Headers.Add("Origin", ApiFactory.AllowedOrigin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await api.ClientWithKey(null).SendAsync(request);

        Assert.Equal(ApiFactory.AllowedOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }
}
