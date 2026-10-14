namespace Signunder.Api.Data;

/// <summary>
/// The model a proxied request was submitted to. fal's result URLs drop the model's last
/// path segment (fal-ai/flux/schnell comes back as fal-ai/flux), so the full name is kept
/// from the submit call for the record.
/// </summary>
public sealed class ProxyRequest
{
    public Guid TenantId { get; init; }
    public required string Provider { get; init; }
    public required string UpstreamId { get; init; }
    public required string Model { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
