using Microsoft.EntityFrameworkCore;
using Signunder.Api.Data;

namespace Signunder.Api.Signing;

/// <summary>
/// Deletes signed copies older than <c>Media:RetentionDays</c> (default 30, SIGNUNDER.md
/// §4.10) and clears their records' link to them. Supabase Storage cannot expire current
/// objects itself, so the API does it: at start-up and then hourly while it is awake. A
/// suspended API runs nothing, so the sweep catches up when it next wakes.
/// </summary>
public sealed class MediaSweep(IServiceScopeFactory scopes, IConfiguration config, ILogger<MediaSweep> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var deleted = await SweepAsync(
                    scope.ServiceProvider.GetRequiredService<RegistryDbContext>(),
                    scope.ServiceProvider.GetRequiredService<IMediaStore>(),
                    DateTimeOffset.UtcNow.AddDays(-config.GetValue("Media:RetentionDays", 30)),
                    stoppingToken);
                if (deleted > 0)
                {
                    logger.LogInformation("Deleted {Count} signed copies past retention", deleted);
                }
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger.LogError(e, "Signed-copy sweep failed; it will try again");
            }
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    /// <summary>Deletes copies of records made before <paramref name="cutoff"/>; returns how many.</summary>
    public static async Task<int> SweepAsync(RegistryDbContext db, IMediaStore media, DateTimeOffset cutoff, CancellationToken ct)
    {
        var total = 0;
        while (true)
        {
            var batch = await db.AssetRecords
                .Where(r => r.RetainedMediaKey != null && r.CreatedAt < cutoff)
                .OrderBy(r => r.CreatedAt)
                .Take(200)
                .ToListAsync(ct);
            if (batch.Count == 0)
            {
                return total;
            }
            foreach (var record in batch)
            {
                await media.DeleteAsync(record.RetainedMediaKey!, ct);
                record.RetainedMediaKey = null;
            }
            await db.SaveChangesAsync(ct);
            total += batch.Count;
        }
    }
}
