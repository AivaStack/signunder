using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Signunder.Sdk;

/// <summary>
/// Calls the SignUnder API. Thread-safe; reuse one instance.
/// </summary>
public sealed class SignunderClient
{
    /// <summary>The API's address when none is given: the early-access service in Frankfurt.</summary>
    public static readonly Uri DefaultBaseAddress = new("https://signunder-staging-api.fly.dev/");

    private readonly HttpClient _http;

    /// <summary>
    /// Uses <paramref name="http"/> as it is: its <see cref="HttpClient.BaseAddress"/> must be
    /// the API's address and it must already send the API key. Register the client with
    /// <see cref="SignunderServiceCollectionExtensions.AddSignunderClient"/> to have both set up.
    /// </summary>
    public SignunderClient(HttpClient http)
    {
        ArgumentNullException.ThrowIfNull(http);
        _http = http;
    }

    /// <summary>Creates a client for the API at <paramref name="baseAddress"/> that sends <paramref name="apiKey"/>.</summary>
    public SignunderClient(Uri baseAddress, string apiKey)
        : this(Configure(new HttpClient(), baseAddress, apiKey))
    {
    }

    /// <summary>Creates a client for the API at <see cref="DefaultBaseAddress"/> that sends <paramref name="apiKey"/>.</summary>
    public SignunderClient(string apiKey)
        : this(DefaultBaseAddress, apiKey)
    {
    }

    /// <summary>
    /// Creates a client from the environment: the key from <c>SIGNUNDER_API_KEY</c>, and the
    /// address from <c>SIGNUNDER_BASE_URL</c> if set, otherwise <see cref="DefaultBaseAddress"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException"><c>SIGNUNDER_API_KEY</c> is not set.</exception>
    public static SignunderClient FromEnvironment()
    {
        var key = Environment.GetEnvironmentVariable("SIGNUNDER_API_KEY");
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("No API key: set SIGNUNDER_API_KEY. Get one at https://signunder.com/dashboard.");
        }
        var url = Environment.GetEnvironmentVariable("SIGNUNDER_BASE_URL");
        return new SignunderClient(string.IsNullOrWhiteSpace(url) ? DefaultBaseAddress : new Uri(url), key);
    }

    internal static HttpClient Configure(HttpClient http, Uri? baseAddress, string? apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        baseAddress ??= DefaultBaseAddress;
        // Relative paths resolve under the base only when it ends in a slash.
        http.BaseAddress = baseAddress.AbsoluteUri.EndsWith('/') ? baseAddress : new Uri(baseAddress.AbsoluteUri + "/");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        return http;
    }

    /// <summary>
    /// Signs an image (<c>image/jpeg</c>, <c>image/png</c> or <c>image/webp</c>) with the tenant's active key
    /// and returns it with its registry record id.
    /// </summary>
    /// <exception cref="SignunderException">The API refused the request.</exception>
    public async Task<SignedAsset> SignAsync(
        Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        using var form = Form(content, fileName, contentType);
        using var response = await _http.PostAsync("v1/sign", form, cancellationToken).ConfigureAwait(false);
        await ThrowUnlessSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        return new SignedAsset(
            await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false),
            response.Content.Headers.ContentType?.MediaType ?? contentType,
            Header(response, "SignUnder-Record-Id"),
            Header(response, "SignUnder-Manifest-Hash"));
    }

    /// <inheritdoc cref="SignAsync(Stream, string, string, CancellationToken)"/>
    public async Task<SignedAsset> SignAsync(
        byte[] content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(content, writable: false);
        return await SignAsync(stream, fileName, contentType, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Signs the JPEG, PNG or WebP image at <paramref name="path"/>, its type read from the file.
    /// </summary>
    /// <exception cref="ArgumentException">The file is not a JPEG, PNG or WebP image.</exception>
    /// <exception cref="SignunderException">The API refused the request.</exception>
    public async Task<SignedAsset> SignAsync(string path, CancellationToken cancellationToken = default)
    {
        var (content, contentType) = await ReadImageAsync(path, cancellationToken).ConfigureAwait(false);
        return await SignAsync(content, Path.GetFileName(path), contentType, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads and validates the Content Credentials in an image. An image without any is
    /// not an error: <see cref="Verification.ManifestFound"/> is false.
    /// </summary>
    /// <exception cref="SignunderException">The API refused the request.</exception>
    public async Task<Verification> VerifyAsync(
        Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        using var form = Form(content, fileName, contentType);
        using var response = await _http.PostAsync("v1/verify", form, cancellationToken).ConfigureAwait(false);
        await ThrowUnlessSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return await ReadAsync(response, SdkJson.Default.Verification, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc cref="VerifyAsync(Stream, string, string, CancellationToken)"/>
    public async Task<Verification> VerifyAsync(
        byte[] content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(content, writable: false);
        return await VerifyAsync(stream, fileName, contentType, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads and validates the Content Credentials in the image at <paramref name="path"/>, its type read from the file.
    /// </summary>
    /// <exception cref="ArgumentException">The file is not a JPEG, PNG or WebP image.</exception>
    /// <exception cref="SignunderException">The API refused the request.</exception>
    public async Task<Verification> VerifyAsync(string path, CancellationToken cancellationToken = default)
    {
        var (content, contentType) = await ReadImageAsync(path, cancellationToken).ConfigureAwait(false);
        return await VerifyAsync(content, Path.GetFileName(path), contentType, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reads one of the tenant's registry records, or null if there is none with that id.</summary>
    /// <exception cref="SignunderException">The API refused the request.</exception>
    public async Task<AssetRecord?> GetRecordAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        using var response = await _http.GetAsync($"v1/records/{Uri.EscapeDataString(id)}", cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        await ThrowUnlessSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return await ReadAsync(response, SdkJson.Default.AssetRecord, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<(byte[] Content, string ContentType)> ReadImageAsync(string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var content = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        return (content, ImageType(content) ?? throw new ArgumentException($"{path} is not a JPEG, PNG or WebP image.", nameof(path)));
    }

    /// <summary>The media type from the file's first bytes, or null if it is not one the API signs.</summary>
    internal static string? ImageType(ReadOnlySpan<byte> head) =>
        head.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF]) ? "image/jpeg"
        : head.StartsWith((ReadOnlySpan<byte>)[0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A]) ? "image/png"
        : head.Length >= 12 && head[..4].SequenceEqual("RIFF"u8) && head[8..12].SequenceEqual("WEBP"u8) ? "image/webp"
        : null;

    private static MultipartFormDataContent Form(Stream content, string fileName, string contentType)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        // The caller owns the stream.
        var file = new StreamContent(new NonDisposingStream(content));
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { file, "file", fileName } };
    }

    private static string Header(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values)
            ? values.First()
            : throw new SignunderException(response.StatusCode, $"The response had no {name} header.");

    private static async Task<T> ReadAsync<T>(
        HttpResponseMessage response, JsonTypeInfo<T> type, CancellationToken cancellationToken) =>
        await response.Content.ReadFromJsonAsync(type, cancellationToken).ConfigureAwait(false)
        ?? throw new SignunderException(response.StatusCode, "The response body was empty.");

    private static async Task ThrowUnlessSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        // Errors are RFC 9457 problem details; auth failures have no body.
        string? detail = null;
        try
        {
            var problem = await response.Content.ReadFromJsonAsync(SdkJson.Default.JsonElement, cancellationToken).ConfigureAwait(false);
            if (problem.ValueKind == JsonValueKind.Object && problem.TryGetProperty("detail", out var text))
            {
                detail = text.GetString();
            }
        }
        catch (JsonException)
        {
        }

        throw new SignunderException(response.StatusCode, detail ?? response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "The API key is missing, unknown or revoked.",
            HttpStatusCode.Forbidden => "The API key does not have the scope this call needs.",
            _ => $"The API answered {(int)response.StatusCode}.",
        });
    }

    private sealed class NonDisposingStream(Stream inner) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => inner.Position = value; }
        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            inner.ReadAsync(buffer, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}

/// <summary>Source-generated serialisation, so the SDK works in trimmed and native AOT apps.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(Verification))]
[JsonSerializable(typeof(AssetRecord))]
[JsonSerializable(typeof(JsonElement))]
internal sealed partial class SdkJson : JsonSerializerContext;
