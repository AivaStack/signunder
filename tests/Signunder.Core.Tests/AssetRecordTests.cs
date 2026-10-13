using System.Security.Cryptography;
using Signunder.Core.Registry;
using Signunder.Core.Verification;

namespace Signunder.Core.Tests;

public class AssetRecordTests
{
    private static readonly TenantKey Key = new()
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        KmsKeyArn = "dev:local",
        Algorithm = "es256",
        CertChainPem = "",
    };

    private static readonly string Report =
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "samples", "sample.jpg.crjson"));

    [Fact]
    public void Record_describes_the_signed_output_without_holding_it()
    {
        byte[] asset = [1, 2, 3, 4];
        byte[] manifestStore = [9, 8, 7];
        var now = new DateTimeOffset(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

        var record = AssetRecord.ForSignedImage(Key, "image/jpeg", asset, manifestStore, Report, ValidationState.Valid, now);

        Assert.Equal(Key.TenantId, record.TenantId);
        Assert.Equal(Key.Id, record.TenantKeyId);
        Assert.Equal(now, record.CreatedAt);
        Assert.Equal("image", record.Modality);
        Assert.Equal(4, record.Size);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(asset)), record.BytesSha256);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(manifestStore)), record.ManifestHash);
        Assert.StartsWith("urn:c2pa:", record.ManifestLabel);
        Assert.Equal(ValidationState.Valid, record.ValidationState);
        Assert.Equal("""["c2pa.created"]""", record.Actions);
        Assert.Equal(Report, record.ValidationReport);
    }

    [Fact]
    public void Ids_are_ulids_that_sort_by_creation_time()
    {
        var earlier = new DateTimeOffset(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);
        var first = AssetRecord.ForSignedImage(Key, "image/png", [1], [2], Report, ValidationState.Valid, earlier);
        var second = AssetRecord.ForSignedImage(Key, "image/png", [1], [2], Report, ValidationState.Valid, earlier.AddSeconds(1));

        Assert.Equal(26, first.Id.Length);
        Assert.NotEqual(first.Id, second.Id);
        Assert.True(string.CompareOrdinal(first.Id, second.Id) < 0);
    }
}
