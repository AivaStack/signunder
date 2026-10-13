using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Signunder.Core.Registry;

namespace Signunder.Api.Tests;

public class ApiTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static MultipartFormDataContent Image(byte[]? bytes = null, string mime = "image/jpeg")
    {
        var file = new ByteArrayContent(bytes ?? [0xFF, 0xD8, 0xFF, 0xD9]);
        file.Headers.ContentType = new MediaTypeHeaderValue(mime);
        return new MultipartFormDataContent { { file, "file", "picture.jpg" } };
    }

    private static string RecordId(HttpResponseMessage response) =>
        response.Headers.GetValues("SignUnder-Record-Id").Single();

    [Fact]
    public async Task Health_needs_no_key()
    {
        var response = await api.ClientWithKey(null).GetAsync("/healthz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-key")]
    [InlineData("su_test_a_well_formed_key_nobody_was_ever_given_00")]
    public async Task Requests_without_a_valid_key_are_unauthorised(string? key)
    {
        var client = api.ClientWithKey(key);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/v1/sign", Image())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/v1/verify", Image())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/v1/records/01ARZ3NDEKTSV4RRFFQ69G5FAV")).StatusCode);
    }

    [Fact]
    public async Task Revoked_key_stops_working()
    {
        var key = await api.AddTenantAsync("revoked", ApiScopes.All, "arn:aws:kms:eu-central-1:1:key/revoked");
        var client = api.ClientWithKey(key);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/v1/sign", Image())).StatusCode);

        await api.RevokeAsync(key);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/v1/sign", Image())).StatusCode);
    }

    [Fact]
    public async Task Key_without_the_scope_is_forbidden()
    {
        var key = await api.AddTenantAsync("read-only", [ApiScopes.RecordsRead], "arn:aws:kms:eu-central-1:1:key/read-only");
        var client = api.ClientWithKey(key);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/v1/sign", Image())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/v1/verify", Image())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/v1/records/01ARZ3NDEKTSV4RRFFQ69G5FAV")).StatusCode);
    }

    [Fact]
    public async Task Sign_returns_the_signed_file_and_writes_a_record()
    {
        var client = api.ClientWithKey(ApiFactory.DemoKey);

        var signed = await client.PostAsync("/v1/sign", Image([1, 2, 3]));

        Assert.Equal(HttpStatusCode.OK, signed.StatusCode);
        Assert.Equal("image/jpeg", signed.Content.Headers.ContentType?.MediaType);
        var bytes = await signed.Content.ReadAsByteArrayAsync();
        Assert.Equal([1, 2, 3, .. "[marked]"u8, .. "[signed]"u8], bytes);

        var record = await client.GetFromJsonAsync<JsonObject>($"/v1/records/{RecordId(signed)}");
        Assert.Equal(RecordId(signed), (string?)record!["id"]);
        Assert.Equal(bytes.Length, (long?)record["size"]);
        Assert.Equal(signed.Headers.GetValues("SignUnder-Manifest-Hash").Single(), (string?)record["manifest_hash"]);
        Assert.Equal("valid", (string?)record["validation_state"]);
        Assert.Equal("c2pa.created", (string?)record["actions"]![0]);
        Assert.NotNull(record["validation_report"]!["manifests"]);
    }

    [Fact]
    public async Task Sign_watermarks_first_and_binds_the_payload_into_the_manifest()
    {
        var client = api.ClientWithKey(await api.AddTenantAsync("marked", ApiScopes.All, "arn:aws:kms:eu-central-1:1:key/marked"));

        var signed = await client.PostAsync("/v1/sign", Image([1, 2, 3]));

        // The sidecar was given the watermarked bytes, not the upload.
        var toSidecar = api.Sidecar.LastRequest!;
        Assert.Equal([1, 2, 3, .. "[marked]"u8], toSidecar.Asset.ToByteArray());

        var payload = api.Watermarker.LastRequest!.Payload;
        Assert.InRange(payload, 0UL, (1UL << 61) - 1);

        var softBinding = JsonNode.Parse(toSidecar.ManifestDefinitionJson)!["assertions"]!.AsArray()
            .Single(a => (string?)a!["label"] == "c2pa.soft-binding")!["data"]!;
        Assert.Equal("com.adobe.trustmark.Q", (string?)softBinding["alg"]);
        var bound = Convert.FromBase64String((string)softBinding["blocks"]![0]!["value"]!);
        Assert.Equal(payload, System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(bound));

        var record = await client.GetFromJsonAsync<JsonObject>($"/v1/records/{RecordId(signed)}");
        Assert.Equal(payload.ToString("x16"), (string?)record!["wm_payload"]);
        Assert.Equal(Convert.ToHexStringLower(FakeWatermarker.Pdq), (string?)record["fingerprint"]);
        Assert.Equal(87, (int?)record["fingerprint_quality"]);
        Assert.True((bool?)record["layers"]!["watermark"]);
        Assert.True((bool?)record["layers"]!["fingerprint"]);
    }

    [Fact]
    public async Task Image_the_watermark_worker_cannot_read_is_a_bad_request()
    {
        var client = api.ClientWithKey(ApiFactory.DemoKey);

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/v1/sign", Image([7]))).StatusCode);
    }

    [Fact]
    public async Task Each_tenant_signs_with_its_own_key_and_reads_only_its_own_records()
    {
        var alpha = api.ClientWithKey(await api.AddTenantAsync("alpha", ApiScopes.All, "arn:aws:kms:eu-central-1:1:key/alpha"));
        var beta = api.ClientWithKey(await api.AddTenantAsync("beta", ApiScopes.All, "arn:aws:kms:eu-central-1:1:key/beta"));

        var alphaSigned = await alpha.PostAsync("/v1/sign", Image());
        Assert.Equal("arn:aws:kms:eu-central-1:1:key/alpha", api.Sidecar.LastSigner!.KeyRef);
        Assert.Equal("chain of alpha", api.Sidecar.LastSigner.CertChainPem);

        var betaSigned = await beta.PostAsync("/v1/sign", Image());
        Assert.Equal("arn:aws:kms:eu-central-1:1:key/beta", api.Sidecar.LastSigner.KeyRef);

        Assert.Equal(HttpStatusCode.OK, (await alpha.GetAsync($"/v1/records/{RecordId(alphaSigned)}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await alpha.GetAsync($"/v1/records/{RecordId(betaSigned)}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await beta.GetAsync($"/v1/records/{RecordId(alphaSigned)}")).StatusCode);
    }

    [Fact]
    public async Task Tenant_without_an_active_key_cannot_sign()
    {
        var client = api.ClientWithKey(await api.AddTenantAsync("keyless", ApiScopes.All, keyArn: null));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsync("/v1/sign", Image())).StatusCode);
    }

    [Fact]
    public async Task Sign_refuses_other_media_types()
    {
        var client = api.ClientWithKey(ApiFactory.DemoKey);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, (await client.PostAsync("/v1/sign", Image(mime: "text/plain"))).StatusCode);
    }

    [Fact]
    public async Task Verify_summarises_a_signed_file_and_reports_an_unsigned_one()
    {
        var client = api.ClientWithKey(ApiFactory.DemoKey);
        var signed = await (await client.PostAsync("/v1/sign", Image())).Content.ReadAsByteArrayAsync();

        var verified = await (await client.PostAsync("/v1/verify", Image(signed))).Content.ReadFromJsonAsync<JsonObject>();
        Assert.True((bool?)verified!["manifest_found"]);
        Assert.Equal("valid", (string?)verified["validation_state"]);
        Assert.Equal("SignUnder DEV - NON-CONFORMANT", (string?)verified["signed_by"]);
        Assert.False((bool?)verified["altered"]);

        var unsigned = await (await client.PostAsync("/v1/verify", Image())).Content.ReadFromJsonAsync<JsonObject>();
        Assert.False((bool?)unsigned!["manifest_found"]);
    }
}
