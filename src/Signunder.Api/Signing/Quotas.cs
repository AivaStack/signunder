using Microsoft.EntityFrameworkCore;
using Signunder.Api.Data;

namespace Signunder.Api.Signing;

/// <summary>
/// Signed outputs a tenant may make per calendar month (UTC), by tier
/// (<c>Quotas:MonthlyOutputs:{tier}</c>). A tier with no entry is
/// unlimited. The count is of registry records, so a burst of simultaneous signs can
/// pass the limit by a few.
/// </summary>
public sealed class Quotas(RegistryDbContext db, IConfiguration config, TimeProvider time)
{
    public async Task<QuotaState> StateAsync(Guid tenantId, CancellationToken ct)
    {
        var now = time.GetUtcNow();
        var start = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var tier = await db.Tenants.Where(t => t.Id == tenantId).Select(t => t.Tier).SingleAsync(ct);
        var limit = config.GetValue<int?>($"Quotas:MonthlyOutputs:{tier}");
        var used = await db.AssetRecords.CountAsync(r => r.TenantId == tenantId && r.CreatedAt >= start, ct);
        return new QuotaState(tier, limit, used, start, start.AddMonths(1));
    }
}

public sealed record QuotaState(string Tier, int? Limit, int Used, DateTimeOffset PeriodStart, DateTimeOffset ResetsAt)
{
    public bool Exhausted => Limit is { } limit && Used >= limit;

    public string Problem =>
        $"This month's allowance of {Limit:N0} signed outputs on the {Tier} plan is used up. It resets on {ResetsAt:d MMMM yyyy} (UTC).";
}
