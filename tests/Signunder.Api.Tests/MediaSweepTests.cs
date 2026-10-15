using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Signunder.Api.Data;
using Signunder.Api.Signing;
using Signunder.Core.Registry;
using Signunder.Core.Verification;

namespace Signunder.Api.Tests;

public class MediaSweepTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static readonly string Report =
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "samples", "sample.jpg.crjson"));

    private async Task<string> AddRecordWithCopy(RegistryDbContext db, TenantKey key, DateTimeOffset created)
    {
        var record = AssetRecord.ForSignedImage(key, "image/png", [1], [2], Report, ValidationState.Valid, created);
        record.RetainedMediaKey = $"{key.TenantId}/{record.Id}.png";
        await api.Media.PutAsync(record.RetainedMediaKey, [1], "image/png", default);
        db.AssetRecords.Add(record);
        await db.SaveChangesAsync();
        return record.Id;
    }

    [Fact]
    public async Task Copies_past_retention_are_deleted_and_their_records_unlinked()
    {
        await api.AddTenantAsync("sweep", ApiScopes.All, "arn:aws:kms:eu-central-1:1:key/sweep");
        await using var scope = api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RegistryDbContext>();
        var key = await db.TenantKeys.FirstAsync(k => k.KmsKeyArn == "arn:aws:kms:eu-central-1:1:key/sweep");
        var now = DateTimeOffset.UtcNow;
        var old = await AddRecordWithCopy(db, key, now.AddDays(-31));
        var recent = await AddRecordWithCopy(db, key, now.AddDays(-1));

        var deleted = await MediaSweep.SweepAsync(db, api.Media, now.AddDays(-30), default);

        Assert.True(deleted >= 1);
        db.ChangeTracker.Clear();
        var oldRecord = await db.AssetRecords.SingleAsync(r => r.Id == old);
        var recentRecord = await db.AssetRecords.SingleAsync(r => r.Id == recent);
        Assert.Null(oldRecord.RetainedMediaKey);
        Assert.False(api.Media.Items.ContainsKey($"{key.TenantId}/{old}.png"));
        Assert.NotNull(recentRecord.RetainedMediaKey);
        Assert.True(api.Media.Items.ContainsKey(recentRecord.RetainedMediaKey!));

        // The record itself stays: only the copy goes.
        Assert.Equal("image/png", oldRecord.Mime);
        Assert.Equal(System.Net.HttpStatusCode.NotFound,
            (await api.CreateClient().GetAsync($"/media/{old}")).StatusCode);
    }
}
