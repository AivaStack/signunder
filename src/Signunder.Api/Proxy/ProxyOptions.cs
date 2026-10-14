namespace Signunder.Api.Proxy;

/// <summary>Configuration section <c>Proxy</c>. The defaults are the providers' real hosts.</summary>
public sealed class ProxyOptions
{
    public FalOptions Fal { get; set; } = new();
    public ReplicateOptions Replicate { get; set; } = new();

    /// <summary>
    /// Refuse provider outputs not served over HTTPS. Turned off only to run the proxy
    /// against a local stand-in for a provider.
    /// </summary>
    public bool RequireHttpsMedia { get; set; } = true;

    /// <summary>Largest provider output the proxy will download.</summary>
    public long MaxDownloadBytes { get; set; } = 32 * 1024 * 1024;

    public sealed class FalOptions
    {
        public Uri QueueBase { get; set; } = new("https://queue.fal.run/");

        /// <summary>Hosts fal serves outputs from; an exact name or <c>*.</c> and a suffix.</summary>
        public string[] MediaHosts { get; set; } = ["fal.media", "*.fal.media"];
    }

    public sealed class ReplicateOptions
    {
        public Uri ApiBase { get; set; } = new("https://api.replicate.com/");

        /// <summary>Hosts Replicate serves outputs from (its HTTP API reference, read 14 Oct 2026).</summary>
        public string[] MediaHosts { get; set; } = ["replicate.delivery", "*.replicate.delivery"];
    }

    public bool HostAllowed(Uri url, IEnumerable<string> hosts) =>
        (url.Scheme == Uri.UriSchemeHttps || (!RequireHttpsMedia && url.Scheme == Uri.UriSchemeHttp)) && hosts.Any(host => host.StartsWith("*.", StringComparison.Ordinal)
            ? url.Host.EndsWith(host[1..], StringComparison.OrdinalIgnoreCase)
            : string.Equals(url.Host, host, StringComparison.OrdinalIgnoreCase));
}
