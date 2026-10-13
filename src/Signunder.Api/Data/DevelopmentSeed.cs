using Microsoft.EntityFrameworkCore;
using Signunder.Core.Registry;

namespace Signunder.Api.Data;

/// <summary>
/// Local development only: applies migrations and makes sure the demo tenant exists with
/// the signer from configuration as its active key and, if configured, a fixed and
/// therefore public API key.
/// </summary>
public static class DevelopmentSeed
{
    public const string DemoTenantSlug = "demo";

    public static async Task RunAsync(RegistryDbContext db, IConfiguration config, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        var now = DateTimeOffset.UtcNow;
        var tenant = await db.Tenants.SingleOrDefaultAsync(t => t.Slug == DemoTenantSlug, ct);
        if (tenant is null)
        {
            tenant = new Tenant { Id = Guid.CreateVersion7(), LegalName = "Demo Tenant (local development)", Slug = DemoTenantSlug, CreatedAt = now };
            db.Tenants.Add(tenant);
        }

        var keyRef = config["Signer:KeyRef"];
        if (!string.IsNullOrEmpty(keyRef))
        {
            var certChainPath = config["Signer:CertChainPath"];
            var certChainPem = string.IsNullOrEmpty(certChainPath) ? "" : await File.ReadAllTextAsync(certChainPath, ct);

            var active = await db.TenantKeys
                .Where(k => k.TenantId == tenant.Id && k.Status == TenantKeyStatus.Active)
                .ToListAsync(ct);
            if (!active.Any(k => k.KmsKeyArn == keyRef && k.CertChainPem == certChainPem))
            {
                active.ForEach(k => k.Status = TenantKeyStatus.Retired);
                db.TenantKeys.Add(new TenantKey
                {
                    Id = Guid.CreateVersion7(),
                    TenantId = tenant.Id,
                    KmsKeyArn = keyRef,
                    Algorithm = config["Signer:Algorithm"] ?? "es256",
                    CertChainPem = certChainPem,
                    Status = TenantKeyStatus.Active,
                    CreatedAt = now,
                });
            }
        }

        var apiKey = config["Development:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            var hash = ApiKey.HashOf(apiKey);
            if (!await db.ApiKeys.AnyAsync(k => k.Hash == hash, ct))
            {
                db.ApiKeys.Add(ApiKey.FromPlaintext(apiKey, tenant.Id, ApiScopes.All, now));
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
