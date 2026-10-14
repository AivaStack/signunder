using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using Signunder.Core.Registry;

namespace Signunder.Api.Tests;

public class ProxyTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private const string FalKey = "Key fal-customer-key";
    private const string ReplicateToken = "Bearer r8_customer_token";
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47];

    /// <summary>A client set up as a proxy-mode customer: their provider key, our key beside it.</summary>
    private HttpClient Customer(string providerAuthorization, string? signunderKey = ApiFactory.DemoKey)
    {
        var client = api.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", providerAuthorization);
        if (signunderKey is not null)
        {
            client.DefaultRequestHeaders.Add("X-SignUnder-Key", signunderKey);
        }
        return client;
    }

    private static StringContent Body(string json) => new(json, Encoding.UTF8, "application/json");

    /// <summary>
    /// fal's bodies as recorded from fal-ai/flux/schnell on 14 Oct 2026, with the request id
    /// and image URL swapped. Note that fal's follow-up URLs drop the model's last path
    /// segment (fal-ai/flux, not fal-ai/flux/schnell).
    /// </summary>
    private void FalRequest(string id, string imageUrl, string contentType = "image/png")
    {
        var urls = $$$"""
            "response_url": "https://queue.fal.run/fal-ai/flux/requests/{{{id}}}",
            "status_url": "https://queue.fal.run/fal-ai/flux/requests/{{{id}}}/status",
            "cancel_url": "https://queue.fal.run/fal-ai/flux/requests/{{{id}}}/cancel"
            """;
        api.Upstream.OnJson("POST", "https://queue.fal.run/fal-ai/flux/schnell", $$$"""
            {"status": "IN_QUEUE", "request_id": "{{{id}}}", {{{urls}}}, "logs": null, "metrics": {},
             "queue_position": 0, "user_queue_position": 0, "user_queued_count": 1}
            """);
        api.Upstream.OnJson("GET", $"https://queue.fal.run/fal-ai/flux/requests/{id}/status", $$$"""
            {"status": "COMPLETED", "request_id": "{{{id}}}", {{{urls}}}, "logs": null,
             "metrics": {"inference_time": 3.1630001068115234}}
            """);
        api.Upstream.OnJson("GET", $"https://queue.fal.run/fal-ai/flux/requests/{id}", $$$"""
            {"images": [{"url": "{{{imageUrl}}}", "width": 512, "height": 512, "content_type": "{{{contentType}}}"}],
             "timings": {"inference": 2.8398281650152057}, "seed": 1182286634,
             "has_nsfw_concepts": [false], "prompt": "a lighthouse on a cliff at dusk, photograph"}
            """);
    }

    [Fact]
    public async Task Fal_queue_round_trip_signs_the_image_and_routes_follow_ups_through_the_proxy()
    {
        FalRequest("req-1", "https://v3b.fal.media/files/b/abc/out.png");
        api.Upstream.OnFile("https://v3b.fal.media/files/b/abc/out.png", Png, "image/png");
        var client = Customer(FalKey);

        var submitted = await (await client.PostAsync("/proxy/fal/queue/fal-ai/flux/schnell", Body("""{"prompt": "a lighthouse"}""")))
            .Content.ReadFromJsonAsync<JsonObject>();

        // The customer's fal key went upstream unchanged; ours did not.
        var submit = api.Upstream.Requests.Last(r => r.Method == HttpMethod.Post);
        Assert.Equal(FalKey, submit.Headers.Authorization!.ToString());
        Assert.False(submit.Headers.Contains("X-SignUnder-Key"));
        var responseUrl = (string)submitted!["response_url"]!;
        Assert.StartsWith("http://localhost/proxy/fal/queue/", responseUrl);
        Assert.StartsWith("http://localhost/proxy/fal/queue/", (string)submitted["status_url"]!);

        Assert.Equal("IN_QUEUE", (string?)submitted["status"]);
        var status = await client.GetFromJsonAsync<JsonObject>((string)submitted["status_url"]!);
        Assert.StartsWith("http://localhost/proxy/fal/queue/", (string)status!["response_url"]!);
        Assert.Equal("COMPLETED", (string?)status!["status"]);

        var result = await client.GetFromJsonAsync<JsonObject>(responseUrl);
        var image = result!["images"]![0]!;
        Assert.Equal(1182286634, (long?)result["seed"]);
        Assert.NotNull(result["timings"]);
        var recordId = (string)image["signunder_record_id"]!;
        Assert.Equal($"http://localhost/media/{recordId}", (string?)image["url"]);

        // The signed copy is served, and it is what the pipeline produced.
        var served = await api.CreateClient().GetAsync($"/media/{recordId}");
        Assert.Equal(HttpStatusCode.OK, served.StatusCode);
        Assert.Equal("image/png", served.Content.Headers.ContentType?.MediaType);
        var servedBytes = await served.Content.ReadAsByteArrayAsync();
        Assert.Equal([.. Png, .. "[marked]"u8, .. "[signed]"u8], servedBytes);

        var record = await api.ClientWithKey(ApiFactory.DemoKey).GetFromJsonAsync<JsonObject>($"/v1/records/{recordId}");
        Assert.Equal("fal", (string?)record!["provider"]);
        // The full model from the submit call, though fal's result URL says fal-ai/flux.
        Assert.Equal("fal-ai/flux/schnell", (string?)record["model_name"]);
        Assert.Equal($"http://localhost/media/{recordId}", (string?)record["media_url"]);
    }

    [Fact]
    public async Task Fetching_a_fal_result_again_returns_the_same_signed_copy()
    {
        FalRequest("req-2", "https://v3.fal.media/files/def/out.png");
        api.Upstream.OnFile("https://v3.fal.media/files/def/out.png", Png, "image/png");
        var client = Customer(FalKey);

        var first = await client.GetFromJsonAsync<JsonObject>("/proxy/fal/queue/fal-ai/flux/requests/req-2");
        var second = await client.GetFromJsonAsync<JsonObject>("/proxy/fal/queue/fal-ai/flux/requests/req-2");

        Assert.Equal((string?)first!["images"]![0]!["url"], (string?)second!["images"]![0]!["url"]);
        Assert.Single(api.Upstream.Requests, r => r.RequestUri!.ToString() == "https://v3.fal.media/files/def/out.png");
    }

    [Theory]
    [InlineData("https://evil.example/out.png", "image/png", "not an allowed fal media host")]
    [InlineData("http://v3.fal.media/files/plain-http.png", "image/png", "not an allowed fal media host")]
    [InlineData("https://v3.fal.media/files/anim.gif", "image/gif", "only JPEG, PNG and WebP")]
    public async Task Fal_output_that_cannot_be_signed_fails_rather_than_pass_unsigned(string url, string type, string reason)
    {
        var id = $"req-bad-{Math.Abs(url.GetHashCode())}";
        FalRequest(id, url, type);
        api.Upstream.OnFile(url, Png, type);

        var response = await Customer(FalKey).GetAsync($"/proxy/fal/queue/fal-ai/flux/requests/{id}");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Contains(reason, await response.Content.ReadAsStringAsync());
        Assert.DoesNotContain(api.Upstream.Requests, r => r.RequestUri!.Host == "evil.example");
    }

    [Fact]
    public async Task Fal_errors_pass_through()
    {
        api.Upstream.OnJson("POST", "https://queue.fal.run/fal-ai/flux/dev", """{"detail": "Invalid key"}""", HttpStatusCode.Unauthorized);

        var response = await Customer("Key wrong").PostAsync("/proxy/fal/queue/fal-ai/flux/dev", Body("{}"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Invalid key", (string?)(await response.Content.ReadFromJsonAsync<JsonObject>())!["detail"]);
    }

    [Fact]
    public async Task Replicate_prediction_with_prefer_wait_signs_every_output_image()
    {
        api.Upstream.On("POST", "https://api.replicate.com/v1/models/black-forest-labs/flux-schnell/predictions", request =>
        {
            Assert.Equal("wait", request.Headers.GetValues("Prefer").Single());
            return FakeUpstream.Json(ReplicatePrediction("pred-1", "succeeded",
                """["https://replicate.delivery/xezq/a/out-0.webp", "https://replicate.delivery/xezq/a/out-1.webp"]"""),
                HttpStatusCode.Created);
        });
        api.Upstream.OnFile("https://replicate.delivery/xezq/a/out-0.webp", [1, 2], "image/webp");
        api.Upstream.OnFile("https://replicate.delivery/xezq/a/out-1.webp", [3, 4], "image/webp");
        var client = Customer(ReplicateToken);
        client.DefaultRequestHeaders.Add("Prefer", "wait");

        var response = await client.PostAsync("/proxy/replicate/v1/models/black-forest-labs/flux-schnell/predictions",
            Body("""{"input": {"prompt": "a lighthouse"}}"""));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var prediction = await response.Content.ReadFromJsonAsync<JsonObject>();
        var output = prediction!["output"]!.AsArray().Select(o => (string)o!).ToList();
        Assert.Equal(2, output.Count);
        Assert.All(output, url => Assert.StartsWith("http://localhost/media/", url));
        Assert.Equal("http://localhost/proxy/replicate/v1/predictions/pred-1", (string?)prediction["urls"]!["get"]);
        Assert.Equal("https://replicate.com/p/pred-1", (string?)prediction["urls"]!["web"]);
        Assert.Equal("succeeded", (string?)prediction["status"]);

        // Replicate's file downloads need the customer's token.
        var download = api.Upstream.Requests.Last(r => r.RequestUri!.Host == "replicate.delivery");
        Assert.Equal(ReplicateToken, download.Headers.Authorization!.ToString());

        var media = await api.CreateClient().GetAsync(new Uri(output[1]).AbsolutePath);
        var mediaBytes = await media.Content.ReadAsByteArrayAsync();
        Assert.Equal([3, 4, .. "[marked]"u8, .. "[signed]"u8], mediaBytes);
    }

    [Fact]
    public async Task Replicate_polling_signs_once_the_prediction_succeeds()
    {
        var calls = 0;
        api.Upstream.On("GET", "https://api.replicate.com/v1/predictions/pred-2", _ => FakeUpstream.Json(++calls == 1
            ? ReplicatePrediction("pred-2", "processing", "null")
            : ReplicatePrediction("pred-2", "succeeded", "\"https://replicate.delivery/b/out.png\"")));
        api.Upstream.OnFile("https://replicate.delivery/b/out.png", Png, "image/png");
        var client = Customer(ReplicateToken);

        var processing = await client.GetFromJsonAsync<JsonObject>("/proxy/replicate/v1/predictions/pred-2");
        Assert.Equal("processing", (string?)processing!["status"]);

        var done = await client.GetFromJsonAsync<JsonObject>("/proxy/replicate/v1/predictions/pred-2");
        var again = await client.GetFromJsonAsync<JsonObject>("/proxy/replicate/v1/predictions/pred-2");
        Assert.StartsWith("http://localhost/media/", (string?)done!["output"]);
        Assert.Equal((string?)done["output"], (string?)again!["output"]);
        Assert.Single(api.Upstream.Requests, r => r.RequestUri!.ToString() == "https://replicate.delivery/b/out.png");
    }

    /// <summary>
    /// Replicate's prediction body as recorded from black-forest-labs/flux-schnell on
    /// 14 Oct 2026, with the id and output swapped. The version is "hidden" for this
    /// official model, and urls carries stream and web links that stay Replicate's own.
    /// </summary>
    private static string ReplicatePrediction(string id, string status, string output) => $$$"""
        {"id": "{{{id}}}", "model": "black-forest-labs/flux-schnell", "version": "hidden",
         "input": {"megapixels": "0.25", "num_outputs": 1, "prompt": "a lighthouse on a cliff at dusk, photograph"},
         "logs": "running quantized prediction\nUsing seed: 4166677529\n", "output": {{{output}}},
         "data_removed": false, "error": null, "source": "api", "status": "{{{status}}}",
         "created_at": "2026-10-14T14:44:47.708Z", "started_at": "2026-10-14T14:44:48.025487958Z",
         "completed_at": "2026-10-14T14:44:48.197034843Z",
         "urls": {"cancel": "https://api.replicate.com/v1/predictions/{{{id}}}/cancel",
                  "get": "https://api.replicate.com/v1/predictions/{{{id}}}",
                  "stream": "https://stream.replicate.com/v1/files/abc",
                  "web": "https://replicate.com/p/{{{id}}}"},
         "metrics": {"image_count": 1, "predict_time": 0.17}}
        """;

    [Fact]
    public async Task Replicate_rate_limit_passes_through()
    {
        // Recorded 14 Oct 2026 from an account holding under $5 of credit.
        api.Upstream.OnJson("POST", "https://api.replicate.com/v1/models/black-forest-labs/flux-dev/predictions", """
            {"detail": "Request was throttled. Your rate limit for creating predictions is reduced to 6 requests per minute with a burst of 1 requests while you have less than $5.0 in credit. Your rate limit resets in ~9s.", "status": 429, "retry_after": 9}
            """, HttpStatusCode.TooManyRequests);

        var response = await Customer(ReplicateToken).PostAsync("/proxy/replicate/v1/models/black-forest-labs/flux-dev/predictions", Body("{}"));

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal(9, (int?)(await response.Content.ReadFromJsonAsync<JsonObject>())!["retry_after"]);
    }

    [Fact]
    public async Task Replicate_output_that_is_not_an_image_is_left_alone()
    {
        api.Upstream.OnJson("GET", "https://api.replicate.com/v1/predictions/pred-3",
            """{"id": "pred-3", "status": "succeeded", "output": ["hello", "https://replicate.delivery/c/notes.txt"]}""");
        api.Upstream.OnFile("https://replicate.delivery/c/notes.txt", "text"u8.ToArray(), "text/plain");

        var prediction = await Customer(ReplicateToken).GetFromJsonAsync<JsonObject>("/proxy/replicate/v1/predictions/pred-3");

        Assert.Equal("hello", (string?)prediction!["output"]![0]);
        Assert.Equal("https://replicate.delivery/c/notes.txt", (string?)prediction["output"]![1]);
    }

    [Fact]
    public async Task Proxy_needs_a_signunder_key_with_the_sign_scope()
    {
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await Customer(FalKey, signunderKey: null).PostAsync("/proxy/fal/queue/fal-ai/flux/schnell", Body("{}"))).StatusCode);

        var readOnly = await api.AddTenantAsync("proxy-read-only", [ApiScopes.RecordsRead], "arn:aws:kms:eu-central-1:1:key/p");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Customer(ReplicateToken, readOnly).GetAsync("/proxy/replicate/v1/predictions/x")).StatusCode);
    }

    [Fact]
    public async Task Unknown_media_is_not_found()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await api.CreateClient().GetAsync("/media/01ARZ3NDEKTSV4RRFFQ69G5FAV")).StatusCode);
    }
}
