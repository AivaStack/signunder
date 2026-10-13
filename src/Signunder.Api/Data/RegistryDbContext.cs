using Microsoft.EntityFrameworkCore;
using Signunder.Core.Registry;

namespace Signunder.Api.Data;

public sealed class RegistryDbContext(DbContextOptions<RegistryDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantKey> TenantKeys => Set<TenantKey>();
    public DbSet<AssetRecord> AssetRecords => Set<AssetRecord>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Tenant>(tenant =>
        {
            tenant.ToTable("tenant");
            tenant.HasIndex(t => t.Slug).IsUnique();
        });

        model.Entity<TenantKey>(key =>
        {
            key.ToTable("tenant_key");
            key.Property(k => k.Status).HasConversion<string>();
            key.HasOne<Tenant>().WithMany().HasForeignKey(k => k.TenantId);
        });

        model.Entity<AssetRecord>(record =>
        {
            record.ToTable("asset_record");
            record.Property(r => r.Id).HasMaxLength(26);
            record.Property(r => r.ValidationState).HasConversion<string>();
            record.Property(r => r.Actions).HasColumnType("jsonb");
            record.Property(r => r.Layers).HasColumnType("jsonb");
            record.Property(r => r.ValidationReport).HasColumnType("jsonb");
            record.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId);
            record.HasOne<TenantKey>().WithMany().HasForeignKey(r => r.TenantKeyId);
            record.HasIndex(r => r.ManifestHash);
            record.HasIndex(r => new { r.TenantId, r.CreatedAt });
        });
    }
}
