namespace Signunder.Core.Registry;

public enum TenantKeyStatus
{
    Pending,
    Active,
    Retired,
}

/// <summary>
/// A tenant's signing key. Only the reference and the certificate are stored; the
/// private key stays in KMS (SIGNUNDER.md §4.5).
/// </summary>
public sealed class TenantKey
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }

    /// <summary>A KMS key ARN, or <c>dev:local</c> for the sidecar's development signer.</summary>
    public required string KmsKeyArn { get; init; }
    public required string Algorithm { get; init; }

    /// <summary>Signing certificate first. Empty for the development signer.</summary>
    public required string CertChainPem { get; init; }
    public TenantKeyStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}
