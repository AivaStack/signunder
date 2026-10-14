using Microsoft.EntityFrameworkCore;
using Signunder.Core.Registry;

namespace Signunder.Api.Data;

public sealed class RegistryDbContext(DbContextOptions<RegistryDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantKey> TenantKeys => Set<TenantKey>();
    public DbSet<AssetRecord> AssetRecords => Set<AssetRecord>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<ProxyResult> ProxyResults => Set<ProxyResult>();
    public DbSet<ProxyRequest> ProxyRequests => Set<ProxyRequest>();

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

        model.Entity<ApiKey>(key =>
        {
            key.ToTable("api_key");
            key.HasOne<Tenant>().WithMany().HasForeignKey(k => k.TenantId);
            key.HasIndex(k => k.Hash).IsUnique();
        });

        model.Entity<ProxyResult>(result =>
        {
            result.ToTable("proxy_result");
            result.HasKey(r => new { r.TenantId, r.Provider, r.UpstreamId });
            result.Property(r => r.Body).HasColumnType("jsonb");
            result.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId);
        });

        model.Entity<ProxyRequest>(request =>
        {
            request.ToTable("proxy_request");
            request.HasKey(r => new { r.TenantId, r.Provider, r.UpstreamId });
            request.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId);
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
            record.HasIndex(r => r.WmPayload).IsUnique();
            record.HasIndex(r => new { r.TenantId, r.CreatedAt });
        });
    }
}
