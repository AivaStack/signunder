using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Signunder.Api.Signing;

namespace Signunder.Api.Tests;

/// <summary>
/// Stands in for fal and Replicate. fal's bodies are recorded from a live call (14 Oct
/// 2026); Replicate's are still shaped from its API documentation (read the same day).
/// </summary>
public sealed class FakeUpstream : HttpMessageHandler
{
    private readonly ConcurrentDictionary<string, Func<HttpRequestMessage, HttpResponseMessage>> _routes = new();

    public ConcurrentQueue<HttpRequestMessage> Requests { get; } = new();

    public void On(string method, string url, Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        _routes[$"{method} {url}"] = respond;

    public void OnJson(string method, string url, string json, HttpStatusCode status = HttpStatusCode.OK) =>
        On(method, url, _ => Json(json, status));

    public void OnFile(string url, byte[] content, string contentType) =>
        On("GET", url, _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(content) { Headers = { ContentType = new(contentType) } },
        });

    public static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Enqueue(request);
        var key = $"{request.Method} {request.RequestUri}";
        return Task.FromResult(_routes.TryGetValue(key, out var respond)
            ? respond(request)
            : new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent($"no fake route for {key}") });
    }
}

public sealed class InMemoryMediaStore : IMediaStore
{
    public ConcurrentDictionary<string, (byte[] Content, string ContentType)> Items { get; } = new();

    public Task PutAsync(string key, byte[] content, string contentType, CancellationToken ct)
    {
        Items[key] = (content, contentType);
        return Task.CompletedTask;
    }

    public Task<(byte[] Content, string ContentType)?> GetAsync(string key, CancellationToken ct) =>
        Task.FromResult(Items.TryGetValue(key, out var item) ? item : ((byte[], string)?)null);
}
