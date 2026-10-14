using Amazon.S3;
using Amazon.S3.Model;

namespace Signunder.Api.Signing;

/// <summary>
/// Holds signed copies that SignUnder serves itself (proxy mode). Copies are transient:
/// the bucket deletes them after 30 days (SIGNUNDER.md §4.10).
/// </summary>
public interface IMediaStore
{
    Task PutAsync(string key, byte[] content, string contentType, CancellationToken ct);

    /// <summary>The stored copy and its type, or null if it is gone.</summary>
    Task<(byte[] Content, string ContentType)?> GetAsync(string key, CancellationToken ct);
}

/// <summary>S3-compatible storage: MinIO locally, Supabase Storage in production.</summary>
public sealed class S3MediaStore(IAmazonS3 s3, IConfiguration config) : IMediaStore
{
    private string Bucket => config["Media:Bucket"] ?? "signed-media";

    public async Task PutAsync(string key, byte[] content, string contentType, CancellationToken ct)
    {
        using var body = new MemoryStream(content, writable: false);
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = Bucket,
            Key = key,
            InputStream = body,
            ContentType = contentType,
        }, ct);
    }

    public async Task<(byte[] Content, string ContentType)?> GetAsync(string key, CancellationToken ct)
    {
        try
        {
            using var response = await s3.GetObjectAsync(Bucket, key, ct);
            using var buffer = new MemoryStream();
            await response.ResponseStream.CopyToAsync(buffer, ct);
            return (buffer.ToArray(), response.Headers.ContentType);
        }
        catch (AmazonS3Exception e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}

/// <summary>Used when no store is configured: nothing can be kept.</summary>
public sealed class NoMediaStore : IMediaStore
{
    public Task PutAsync(string key, byte[] content, string contentType, CancellationToken ct) =>
        throw new InvalidOperationException("No media store is configured (Media:ServiceUrl).");

    public Task<(byte[] Content, string ContentType)?> GetAsync(string key, CancellationToken ct) =>
        Task.FromResult<(byte[], string)?>(null);
}
