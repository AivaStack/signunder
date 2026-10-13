using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Signunder.Core.Registry;
using Signunder.Sdk;

namespace Signunder.Api.Tests;

/// <summary>The SDK against the real API, as a customer would use it.</summary>
public class SdkTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xD9];

    private SignunderClient Client(string? apiKey = ApiFactory.DemoKey) => new(api.ClientWithKey(apiKey));

    [Fact]
    public async Task Sign_then_read_the_record()
    {
        var client = Client();

        var signed = await client.SignAsync(Jpeg, "picture.jpg", "image/jpeg");

        Assert.Equal([.. Jpeg, .. "[signed]"u8], signed.Content);
        Assert.Equal("image/jpeg", signed.ContentType);
        Assert.Equal(26, signed.RecordId.Length);

        var record = await client.GetRecordAsync(signed.RecordId);
        Assert.NotNull(record);
        Assert.Equal(signed.RecordId, record.Id);
        Assert.Equal(signed.ManifestHash, record.ManifestHash);
        Assert.Equal(signed.Content.Length, record.Size);
        Assert.Equal("image", record.Modality);
        Assert.Equal("valid", record.ValidationState);
        Assert.Equal(["c2pa.created"], record.Actions);
        Assert.True(record.Layers["manifest"]);
        Assert.False(record.Layers["watermark"]);
        Assert.True(record.ValidationReport.TryGetProperty("manifests", out _));
    }

    [Fact]
    public async Task Sign_leaves_the_callers_stream_open()
    {
        using var stream = new MemoryStream(Jpeg);

        await Client().SignAsync(stream, "picture.jpg", "image/jpeg");

        Assert.True(stream.CanRead);
    }

    [Fact]
    public async Task Verify_a_signed_and_an_unsigned_image()
    {
        var client = Client();
        var signed = await client.SignAsync(Jpeg, "picture.jpg", "image/jpeg");

        var verified = await client.VerifyAsync(signed.Content, "picture.jpg", "image/jpeg");
        Assert.True(verified.ManifestFound);
        Assert.Equal("valid", verified.ValidationState);
        Assert.Equal("SignUnder DEV - NON-CONFORMANT", verified.SignedBy);
        Assert.Equal("untrusted", verified.TrustStatus);
        Assert.Equal(["c2pa.created"], verified.Actions);
        Assert.False(verified.Altered);
        Assert.Equal(["signingCredential.untrusted"], verified.Failures);
        Assert.StartsWith("urn:c2pa:", verified.ManifestLabel);
        Assert.NotNull(verified.CertSubject);
        Assert.NotNull(verified.Report);

        var unsigned = await client.VerifyAsync(Jpeg, "picture.jpg", "image/jpeg");
        Assert.False(unsigned.ManifestFound);
        Assert.Null(unsigned.ValidationState);
    }

    [Fact]
    public async Task Unknown_record_is_null()
    {
        Assert.Null(await Client().GetRecordAsync("01ARZ3NDEKTSV4RRFFQ69G5FAV"));
    }

    [Fact]
    public async Task Refusals_carry_the_status_and_the_reason()
    {
        var unauthorised = await Assert.ThrowsAsync<SignunderException>(
            () => Client("su_test_a_well_formed_key_nobody_was_ever_given_00").SignAsync(Jpeg, "picture.jpg", "image/jpeg"));
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorised.StatusCode);
        Assert.Contains("API key", unauthorised.Message);

        var readOnly = await api.AddTenantAsync("sdk-read-only", [ApiScopes.RecordsRead], "arn:aws:kms:eu-central-1:1:key/sdk");
        var forbidden = await Assert.ThrowsAsync<SignunderException>(
            () => Client(readOnly).SignAsync(Jpeg, "picture.jpg", "image/jpeg"));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var unsupported = await Assert.ThrowsAsync<SignunderException>(
            () => Client().SignAsync(Jpeg, "notes.txt", "text/plain"));
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, unsupported.StatusCode);
        Assert.Equal("Only image/jpeg and image/png are supported.", unsupported.Message);
    }

    [Fact]
    public void Registration_sets_the_address_and_the_key()
    {
        var services = new ServiceCollection();
        services.AddSignunderClient(o =>
        {
            o.BaseAddress = new Uri("https://api.example.test/");
            o.ApiKey = "su_test_key";
        });

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<SignunderClient>());

        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddSignunderClient(o => o.ApiKey = "su_test_key")
            .Services.BuildServiceProvider().GetRequiredService<SignunderClient>());
    }
}
