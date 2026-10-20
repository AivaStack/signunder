using Microsoft.EntityFrameworkCore;
using Signunder.Api.Data;

namespace Signunder.Api.Signing;

/// <summary>
/// Signed outputs a tenant may make per calendar month (UTC), by tier
/// (<c>Quotas:MonthlyOutputs:{tier}</c>). A tier with no entry is
/// unlimited; a tier with <c>Quotas:Overage:{tier}</c> set keeps signing past its limit,
/// and the extra outputs are counted for billing (ADR 0015). The count is of registry records, so a burst of simultaneous signs can
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
        return new QuotaState(tier, limit, used, start, start.AddMonths(1), config.GetValue<bool>($"Quotas:Overage:{tier}"));
    }
}

public sealed record QuotaState(string Tier, int? Limit, int Used, DateTimeOffset PeriodStart, DateTimeOffset ResetsAt, bool OverageAllowed = false)
{
    public bool Exhausted => !OverageAllowed && Limit is { } limit && Used >= limit;

    /// <summary>Outputs this month beyond the limit; only a plan with overage gets any.</summary>
    public int Overage => Limit is { } limit ? Math.Max(0, Used - limit) : 0;

    public string Problem =>
        $"This month's allowance of {Limit:N0} signed outputs on the {Tier} plan is used up. It resets on {ResetsAt:d MMMM yyyy} (UTC).";
}
