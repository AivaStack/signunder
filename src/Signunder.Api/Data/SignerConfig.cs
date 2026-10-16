using Signunder.Core.Registry;

namespace Signunder.Api.Data;

/// <summary>
/// The signer named in configuration (<c>Signer:*</c>). Until tenants are listed and sign
/// under their own certificates (SIGNUNDER.md §6, Phase 3), every tenant signs with it.
/// </summary>
public static class SignerConfig
{
    /// <summary>A new active key for <paramref name="tenantId"/> using the configured signer, or null if none is configured.</summary>
    public static async Task<TenantKey?> NewTenantKeyAsync(IConfiguration config, Guid tenantId, DateTimeOffset now, CancellationToken ct)
    {
        var keyRef = config["Signer:KeyRef"];
        if (string.IsNullOrEmpty(keyRef))
        {
            return null;
        }
        var certChainPath = config["Signer:CertChainPath"];
        return new TenantKey
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            KmsKeyArn = keyRef,
            Algorithm = config["Signer:Algorithm"] ?? "es256",
            CertChainPem = string.IsNullOrEmpty(certChainPath) ? "" : await File.ReadAllTextAsync(certChainPath, ct),
            Status = TenantKeyStatus.Active,
            CreatedAt = now,
        };
    }
}
