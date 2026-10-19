using System.Text.Json;

namespace Signunder.Api.Account;

/// <summary>
/// Reads a domain's TXT records over DNS-over-HTTPS (Cloudflare's resolver,
/// <c>Identity:DnsResolver</c>), since .NET has no TXT lookup of its own.
/// </summary>
public sealed class DomainVerifier(IHttpClientFactory clients, IConfiguration config)
{
    public const string Client = "dns-over-https";

    private string Resolver => config["Identity:DnsResolver"] ?? "https://cloudflare-dns.com/dns-query";

    /// <summary>The TXT records on <paramref name="domain"/>; empty if there are none or the lookup fails.</summary>
    public async Task<IReadOnlyList<string>> TxtRecordsAsync(string domain, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{Resolver}?name={Uri.EscapeDataString(domain)}&type=TXT");
        request.Headers.Accept.ParseAdd("application/dns-json");
        try
        {
            using var response = await clients.CreateClient(Client).SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }
            using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (!body.RootElement.TryGetProperty("Answer", out var answers))
            {
                return [];
            }
            return [.. answers.EnumerateArray()
                // 16 is TXT; a long record arrives as quoted strings to join.
                .Where(a => a.TryGetProperty("type", out var type) && type.GetInt32() == 16)
                .Select(a => string.Concat(
                    (a.GetProperty("data").GetString() ?? "").Split('"', StringSplitOptions.RemoveEmptyEntries)
                        .Where(part => !string.IsNullOrWhiteSpace(part))))];
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or TaskCanceledException)
        {
            return [];
        }
    }
}
