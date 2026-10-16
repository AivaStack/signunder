using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Signunder.Api.Auth;
using Signunder.Api.Data;
using Signunder.Api.Signing;
using Signunder.Core.Registry;

namespace Signunder.Api.Proxy;

/// <summary>
/// Proxy mode (SIGNUNDER.md §4.3): the customer points their fal or Replicate client at
/// SignUnder, which forwards each call with the customer's own provider key and signs
/// the images in the results before they come back. The SignUnder key travels in
/// <c>X-SignUnder-Key</c>, because <c>Authorization</c> carries the provider's key.
/// </summary>
public static partial class ProxyEndpoints
{
    public const string Upstream = "proxy-upstream";

    public static IServiceCollection AddProxy(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<ProxyOptions>(config.GetSection("Proxy"));
        // No redirects: a redirect could send a download to a host the allowlist refused.
        services.AddHttpClient(Upstream)
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false })
            .ConfigureHttpClient(http => http.Timeout = TimeSpan.FromSeconds(90));
        services.AddScoped<ProxySigner>();
        return services;
    }

    public static void MapProxy(this IEndpointRouteBuilder app)
    {
        app.MapMethods("/proxy/fal/queue/{**path}", ["GET", "POST", "PUT", "DELETE"],
            (string path, HttpContext http, ProxySigner proxy, CancellationToken ct) => proxy.FalAsync(path, http, ct))
            .RequireAuthorization(ApiScopes.Sign)
            .RequireRateLimiting(RateLimits.PerKey);

        app.MapPost("/proxy/replicate/v1/predictions",
            (HttpContext http, ProxySigner proxy, CancellationToken ct) => proxy.ReplicateAsync("v1/predictions", http, ct))
            .RequireAuthorization(ApiScopes.Sign)
            .RequireRateLimiting(RateLimits.PerKey);
        app.MapPost("/proxy/replicate/v1/models/{owner}/{name}/predictions",
            (string owner, string name, HttpContext http, ProxySigner proxy, CancellationToken ct) =>
                proxy.ReplicateAsync($"v1/models/{owner}/{name}/predictions", http, ct))
            .RequireAuthorization(ApiScopes.Sign)
            .RequireRateLimiting(RateLimits.PerKey);
        app.MapGet("/proxy/replicate/v1/predictions/{id}",
            (string id, HttpContext http, ProxySigner proxy, CancellationToken ct) => proxy.ReplicateAsync($"v1/predictions/{id}", http, ct))
            .RequireAuthorization(ApiScopes.Sign)
            .RequireRateLimiting(RateLimits.PerKey);
        app.MapPost("/proxy/replicate/v1/predictions/{id}/cancel",
            (string id, HttpContext http, ProxySigner proxy, CancellationToken ct) => proxy.ReplicateAsync($"v1/predictions/{id}/cancel", http, ct))
            .RequireAuthorization(ApiScopes.Sign)
            .RequireRateLimiting(RateLimits.PerKey);
    }

    /// <summary>fal's result path: <c>{model}/requests/{id}</c>, without a trailing action.</summary>
    [GeneratedRegex(@"^(?<model>.+)/requests/(?<id>[^/]+)$")]
    internal static partial Regex FalResultPath();
}

/// <summary>A provider output the proxy could not sign; the call fails rather than pass it on unsigned.</summary>
public sealed class UnsignableOutputException(string message, int status = 502) : Exception(message)
{
    /// <summary>502, or 429 when the tenant's monthly allowance is used up.</summary>
    public int Status { get; } = status;
}

public sealed class ProxySigner(
    IHttpClientFactory clients,
    IOptions<ProxyOptions> options,
    ImageSigner signer,
    Quotas quotas,
    RegistryDbContext db)
{
    private ProxyOptions Options => options.Value;

    /// <summary>
    /// Refuses a new generation once the month's allowance is used up, before the provider
    /// makes (and charges for) an image SignUnder would then refuse to sign.
    /// </summary>
    private async Task<IResult?> RefusedOverQuotaAsync(Guid tenantId, HttpContext http, string path, CancellationToken ct)
    {
        if (http.Request.Method != "POST" || path.EndsWith("/cancel", StringComparison.Ordinal))
        {
            return null;
        }
        var quota = await quotas.StateAsync(tenantId, ct);
        return quota.Exhausted ? Results.Problem(quota.Problem, statusCode: 429) : null;
    }

    public async Task<IResult> FalAsync(string path, HttpContext http, CancellationToken ct)
    {
        var tenantId = http.User.TenantId();
        var ours = $"{http.Request.Scheme}://{http.Request.Host}/proxy/fal/queue/";
        var result = http.Request.Method == "GET" ? ProxyEndpoints.FalResultPath().Match(path) : null;

        if (result is { Success: true } && await CachedAsync(tenantId, "fal", result.Groups["id"].Value, ct) is { } cached)
        {
            return Json(HttpStatusCode.OK, cached);
        }

        if (await RefusedOverQuotaAsync(tenantId, http, path, ct) is { } refused)
        {
            return refused;
        }
        var upstream = new Uri(Options.Fal.QueueBase, path + http.Request.QueryString);
        using var response = await ForwardAsync(upstream, http, ["Authorization"], ct);
        var body = await ReadJsonAsync(response, ct);
        if (body is null)
        {
            return await PassThroughAsync(response, ct);
        }
        RewriteUrls(body, Options.Fal.QueueBase.ToString(), ours);

        // A submit: remember which model it went to (see ProxyRequest).
        if (http.Request.Method == "POST" && response.IsSuccessStatusCode && (string?)body["request_id"] is { } submittedId)
        {
            db.ProxyRequests.Add(new ProxyRequest
            {
                TenantId = tenantId,
                Provider = "fal",
                UpstreamId = submittedId,
                Model = path.Trim('/'),
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync(ct);
        }

        if (result is { Success: true } && response.IsSuccessStatusCode)
        {
            var requestId = result.Groups["id"].Value;
            var model = await db.ProxyRequests.AsNoTracking()
                .Where(r => r.TenantId == tenantId && r.Provider == "fal" && r.UpstreamId == requestId)
                .Select(r => r.Model)
                .SingleOrDefaultAsync(ct) ?? result.Groups["model"].Value;
            try
            {
                await SignFalImagesAsync(body, tenantId, model, http, ct);
            }
            catch (UnsignableOutputException e)
            {
                return Results.Problem(e.Message, statusCode: e.Status);
            }
            return Json(response.StatusCode, await RememberAsync(tenantId, "fal", result.Groups["id"].Value, body.ToJsonString(), ct));
        }
        return Json(response.StatusCode, body.ToJsonString());
    }

    public async Task<IResult> ReplicateAsync(string path, HttpContext http, CancellationToken ct)
    {
        var tenantId = http.User.TenantId();
        var ours = $"{http.Request.Scheme}://{http.Request.Host}/proxy/replicate/";

        if (await RefusedOverQuotaAsync(tenantId, http, path, ct) is { } refused)
        {
            return refused;
        }
        var upstream = new Uri(Options.Replicate.ApiBase, path + http.Request.QueryString);
        using var response = await ForwardAsync(upstream, http, ["Authorization", "Prefer"], ct);
        var body = await ReadJsonAsync(response, ct);
        if (body is null)
        {
            return await PassThroughAsync(response, ct);
        }
        RewriteUrls(body, Options.Replicate.ApiBase.ToString(), ours);

        if (response.IsSuccessStatusCode && body is JsonObject prediction
            && (string?)prediction["status"] == "succeeded" && (string?)prediction["id"] is { } id)
        {
            if (await CachedAsync(tenantId, "replicate", id, ct) is { } cached)
            {
                return Json(response.StatusCode, cached);
            }
            var model = (string?)prediction["model"] ?? (string?)prediction["version"];
            try
            {
                prediction["output"] = await SignReplicateOutputAsync(prediction["output"], tenantId, model, http, ct);
            }
            catch (UnsignableOutputException e)
            {
                return Results.Problem(e.Message, statusCode: e.Status);
            }
            return Json(response.StatusCode, await RememberAsync(tenantId, "replicate", id, body.ToJsonString(), ct));
        }
        return Json(response.StatusCode, body.ToJsonString());
    }

    /// <summary>fal image models return <c>images: [{url, content_type, …}]</c>; some return one <c>image</c>.</summary>
    private async Task SignFalImagesAsync(JsonNode body, Guid tenantId, string model, HttpContext http, CancellationToken ct)
    {
        var images = new List<JsonObject>();
        if (body["images"] is JsonArray list)
        {
            images.AddRange(list.OfType<JsonObject>());
        }
        if (body["image"] is JsonObject single)
        {
            images.Add(single);
        }

        foreach (var image in images)
        {
            if ((string?)image["url"] is not { } url || !Uri.TryCreate(url, UriKind.Absolute, out var source))
            {
                throw new UnsignableOutputException("A fal image has no usable url.");
            }
            if (!Options.HostAllowed(source, Options.Fal.MediaHosts))
            {
                throw new UnsignableOutputException($"fal returned an image from {source.Host}, which is not an allowed fal media host.");
            }
            var (bytes, mime) = await DownloadAsync(source, authorization: null, ct);
            if (mime is null || !ImageSigner.SupportedTypes.Contains(mime))
            {
                throw new UnsignableOutputException($"fal returned {mime ?? "an unknown type"}; only JPEG, PNG and WebP can be signed.");
            }
            var record = await SignAsync(tenantId, bytes, mime, source, "fal", model, ct);
            image["url"] = MediaUrl(http, record);
            image["content_type"] = mime;
            image["signunder_record_id"] = record.Id;
        }
    }

    /// <summary>
    /// Replicate's <c>output</c> is model-specific: a URL, a list, or an object. Every URL on
    /// Replicate's media hosts is fetched; images are signed, anything else is left alone.
    /// </summary>
    private async Task<JsonNode?> SignReplicateOutputAsync(JsonNode? output, Guid tenantId, string? model, HttpContext http, CancellationToken ct)
    {
        switch (output)
        {
            case JsonValue value when value.TryGetValue<string>(out var text)
                && Uri.TryCreate(text, UriKind.Absolute, out var source)
                && Options.HostAllowed(source, Options.Replicate.MediaHosts):
                var (bytes, mime) = await DownloadAsync(source, http.Request.Headers.Authorization.ToString(), ct);
                if (mime is null || !mime.StartsWith("image/", StringComparison.Ordinal))
                {
                    return output;
                }
                if (!ImageSigner.SupportedTypes.Contains(mime))
                {
                    throw new UnsignableOutputException($"Replicate returned {mime}; only JPEG, PNG and WebP can be signed.");
                }
                var record = await SignAsync(tenantId, bytes, mime, source, "replicate", model, ct);
                return JsonValue.Create(MediaUrl(http, record));
            case JsonArray array:
                var signed = new JsonArray();
                foreach (var item in array)
                {
                    signed.Add(await SignReplicateOutputAsync(item?.DeepClone(), tenantId, model, http, ct));
                }
                return signed;
            case JsonObject map:
                var signedMap = new JsonObject();
                foreach (var (name, item) in map)
                {
                    signedMap[name] = await SignReplicateOutputAsync(item?.DeepClone(), tenantId, model, http, ct);
                }
                return signedMap;
            default:
                return output;
        }
    }

    private async Task<AssetRecord> SignAsync(
        Guid tenantId, byte[] bytes, string mime, Uri source, string provider, string? model, CancellationToken ct)
    {
        var outcome = await signer.SignAsync(
            tenantId, ByteString.CopyFrom(bytes), mime, Path.GetFileName(source.AbsolutePath),
            new SignOrigin(provider, model), keepCopy: true, ct);
        return outcome.Record ?? throw (outcome.Status == 429
            ? new UnsignableOutputException(outcome.Problem!, 429)
            : new UnsignableOutputException($"Signing failed: {outcome.Problem}"));
    }

    private async Task<(byte[] Bytes, string? Mime)> DownloadAsync(Uri source, string? authorization, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, source);
        if (!string.IsNullOrEmpty(authorization))
        {
            request.Headers.TryAddWithoutValidation("Authorization", authorization);
        }
        using var response = await clients.CreateClient(ProxyEndpoints.Upstream)
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new UnsignableOutputException($"Fetching the output from {source.Host} failed: {(int)response.StatusCode}.");
        }
        if (response.Content.Headers.ContentLength > Options.MaxDownloadBytes)
        {
            throw new UnsignableOutputException("The output is larger than the proxy accepts.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > Options.MaxDownloadBytes)
            {
                throw new UnsignableOutputException("The output is larger than the proxy accepts.");
            }
            buffer.Write(chunk, 0, read);
        }
        return (buffer.ToArray(), response.Content.Headers.ContentType?.MediaType);
    }

    private async Task<HttpResponseMessage> ForwardAsync(Uri upstream, HttpContext http, string[] headers, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(new HttpMethod(http.Request.Method), upstream);
        foreach (var name in headers)
        {
            if (http.Request.Headers.TryGetValue(name, out var value))
            {
                request.Headers.TryAddWithoutValidation(name, value.ToString());
            }
        }
        if (http.Request.ContentLength > 0 || http.Request.Headers.TransferEncoding.Count > 0)
        {
            using var body = new MemoryStream();
            await http.Request.Body.CopyToAsync(body, ct);
            request.Content = new ByteArrayContent(body.ToArray());
            if (http.Request.ContentType is { } type)
            {
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(type);
            }
        }
        return await clients.CreateClient(ProxyEndpoints.Upstream).SendAsync(request, ct);
    }

    private static async Task<JsonNode?> ReadJsonAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.Content.Headers.ContentType?.MediaType != "application/json")
        {
            return null;
        }
        try
        {
            return JsonNode.Parse(await response.Content.ReadAsStringAsync(ct));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static async Task<IResult> PassThroughAsync(HttpResponseMessage response, CancellationToken ct) =>
        new RawResult((int)response.StatusCode, response.Content.Headers.ContentType?.ToString(),
            await response.Content.ReadAsByteArrayAsync(ct));

    /// <summary>A provider response that is not JSON, passed back as it came.</summary>
    private sealed class RawResult(int status, string? contentType, byte[] body) : IResult
    {
        public async Task ExecuteAsync(HttpContext http)
        {
            http.Response.StatusCode = status;
            if (contentType is not null)
            {
                http.Response.ContentType = contentType;
            }
            await http.Response.Body.WriteAsync(body);
        }
    }

    private static IResult Json(HttpStatusCode status, string body) =>
        Results.Text(body, "application/json", statusCode: (int)status);

    /// <summary>Points the provider's own follow-up URLs (status, result, cancel) back through the proxy.</summary>
    internal static void RewriteUrls(JsonNode? node, string from, string to)
    {
        switch (node)
        {
            case JsonObject map:
                foreach (var name in map.Select(p => p.Key).ToList())
                {
                    if (map[name] is JsonValue value && value.TryGetValue<string>(out var text) && text.StartsWith(from, StringComparison.Ordinal))
                    {
                        map[name] = to + text[from.Length..];
                    }
                    else
                    {
                        RewriteUrls(map[name], from, to);
                    }
                }
                break;
            case JsonArray array:
                for (var i = 0; i < array.Count; i++)
                {
                    if (array[i] is JsonValue value && value.TryGetValue<string>(out var text) && text.StartsWith(from, StringComparison.Ordinal))
                    {
                        array[i] = to + text[from.Length..];
                    }
                    else
                    {
                        RewriteUrls(array[i], from, to);
                    }
                }
                break;
        }
    }

    private static string MediaUrl(HttpContext http, AssetRecord record) =>
        $"{http.Request.Scheme}://{http.Request.Host}/media/{record.Id}";

    private async Task<string?> CachedAsync(Guid tenantId, string provider, string id, CancellationToken ct) =>
        await db.ProxyResults.AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.Provider == provider && r.UpstreamId == id)
            .Select(r => r.Body)
            .SingleOrDefaultAsync(ct);

    /// <summary>Stores the signed result; if a concurrent fetch got there first, returns that one instead.</summary>
    private async Task<string> RememberAsync(Guid tenantId, string provider, string id, string body, CancellationToken ct)
    {
        db.ProxyResults.Add(new ProxyResult
        {
            TenantId = tenantId,
            Provider = provider,
            UpstreamId = id,
            Body = body,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        try
        {
            await db.SaveChangesAsync(ct);
            return body;
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            db.ChangeTracker.Clear();
            return await CachedAsync(tenantId, provider, id, ct) ?? body;
        }
    }
}
