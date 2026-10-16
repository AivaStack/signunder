using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Signunder.Api.Tests;

public class PublicVerifyTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private const string Tenant = "demo";

    private static MultipartFormDataContent Image(byte[] bytes)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        return new MultipartFormDataContent { { file, "file", "picture.jpg" } };
    }

    /// <summary>Signs an image as the demo tenant; returns the signed bytes and the record id.</summary>
    private async Task<(byte[] Signed, string RecordId)> SignAsync(byte[] original)
    {
        var response = await api.ClientWithKey(ApiFactory.DemoKey).PostAsync("/v1/sign", Image(original));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadAsByteArrayAsync(), response.Headers.GetValues("SignUnder-Record-Id").Single());
    }

    private async Task<JsonObject> VerifyAsync(byte[] bytes, string tenant = Tenant)
    {
        var response = await api.ClientWithKey(null).PostAsync($"/public/v1/{tenant}/verify", Image(bytes));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonObject>())!;
    }

    [Fact]
    public async Task A_signed_output_is_found_by_its_manifest_without_a_key()
    {
        var (signed, id) = await SignAsync([10, 1]);

        var result = await VerifyAsync(signed);

        Assert.True((bool?)result["ours"]);
        Assert.Equal("manifest", (string?)result["method"]);
        Assert.Equal(id, (string?)result["record"]!["id"]);
        Assert.Equal(Tenant, (string?)result["tenant"]!["slug"]);
        Assert.True((bool?)result["manifest"]!["found"]);
        Assert.False((bool?)result["manifest"]!["altered"]);
    }

    [Fact]
    public async Task With_the_manifest_stripped_the_watermark_finds_the_record()
    {
        var (signed, id) = await SignAsync([10, 2]);
        var stripped = signed[..^"[signed]"u8.Length];

        var result = await VerifyAsync(stripped);

        Assert.True((bool?)result["ours"]);
        Assert.Equal("watermark", (string?)result["method"]);
        Assert.Equal(id, (string?)result["record"]!["id"]);
        Assert.False((bool?)result["manifest"]!["found"]);
    }

    [Fact]
    public async Task With_manifest_and_watermark_gone_a_close_fingerprint_finds_the_record()
    {
        await SignAsync([10, 3]);

        var result = await VerifyAsync([.. FakeWatermarker.NearCopy, 1, 2]);

        Assert.True((bool?)result["ours"]);
        Assert.Equal("fingerprint", (string?)result["method"]);
        Assert.Equal(3, (int?)result["fingerprint_distance"]);
        Assert.True((bool?)result["checks"]!["fingerprint"]);
    }

    [Fact]
    public async Task An_unrelated_image_is_not_claimed()
    {
        await SignAsync([10, 4]);

        var result = await VerifyAsync([7, 7, 7]);

        Assert.False((bool?)result["ours"]);
        Assert.Null(result["method"]);
        Assert.Null(result["record"]);
    }

    [Fact]
    public async Task Another_tenant_never_claims_the_output()
    {
        var (signed, _) = await SignAsync([10, 5]);
        await api.AddTenantAsync("someone-else", Core.Registry.ApiScopes.All, "arn:aws:kms:eu-central-1:1:key/e");

        var result = await VerifyAsync(signed, "someone-else");

        Assert.False((bool?)result["ours"]);
        // The manifest is still described: it is in the file for anyone to read.
        Assert.True((bool?)result["manifest"]!["found"]);
    }

    [Fact]
    public async Task An_empty_or_unreadable_file_gets_a_plain_answer()
    {
        var client = api.ClientWithKey(null);

        var empty = await client.PostAsync($"/public/v1/{Tenant}/verify", Image([]));
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        Assert.Contains("empty", (string?)(await empty.Content.ReadFromJsonAsync<JsonObject>())!["detail"]);

        // The fake watermarker refuses a one-byte image, as the real one refuses junk.
        var junk = await client.PostAsync($"/public/v1/{Tenant}/verify", Image([0x42]));
        Assert.Equal(HttpStatusCode.BadRequest, junk.StatusCode);
        var detail = (string?)(await junk.Content.ReadFromJsonAsync<JsonObject>())!["detail"];
        Assert.StartsWith("This file could not be read as a JPEG image", detail);
        Assert.DoesNotContain("could not read the image", detail);
    }

    [Fact]
    public async Task The_public_record_leaves_out_marks_and_internal_ids()
    {
        var (_, id) = await SignAsync([10, 6]);
        var client = api.ClientWithKey(null);

        var record = await client.GetFromJsonAsync<JsonObject>($"/public/v1/{Tenant}/records/{id}");

        Assert.Equal(id, (string?)record!["id"]);
        Assert.Equal("Demo Tenant (local development)", (string?)record["tenant"]!["name"]);
        Assert.NotNull(record["signed_by"]);
        Assert.False(record.ContainsKey("wm_payload"));
        Assert.False(record.ContainsKey("fingerprint"));
        Assert.False(record.ContainsKey("tenant_id"));
        Assert.False(record.ContainsKey("validation_report"));

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/public/v1/no-such-tenant/records/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/public/v1/{Tenant}/records/01ARZ3NDEKTSV4RRFFQ69G5FAV")).StatusCode);
    }

    [Fact]
    public async Task Only_the_configured_origin_may_call_from_a_browser()
    {
        async Task<string?> AllowedOrigin(string origin)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"/public/v1/{Tenant}/records/01ARZ3NDEKTSV4RRFFQ69G5FAV");
            request.Headers.Add("Origin", origin);
            var response = await api.ClientWithKey(null).SendAsync(request);
            return response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values) ? values.Single() : null;
        }

        Assert.Equal(ApiFactory.AllowedOrigin, await AllowedOrigin(ApiFactory.AllowedOrigin));
        Assert.Null(await AllowedOrigin("https://elsewhere.example"));
    }
}
