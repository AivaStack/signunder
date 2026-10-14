namespace Signunder.Api.Data;

/// <summary>
/// A provider result that proxy mode has already signed, kept so that fetching it again
/// returns the same signed copies instead of signing (and recording) them twice.
/// </summary>
public sealed class ProxyResult
{
    public Guid TenantId { get; init; }

    /// <summary><c>fal</c> or <c>replicate</c>.</summary>
    public required string Provider { get; init; }

    /// <summary>fal's request id or Replicate's prediction id.</summary>
    public required string UpstreamId { get; init; }

    /// <summary>The rewritten response body, as returned to the caller.</summary>
    public required string Body { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
