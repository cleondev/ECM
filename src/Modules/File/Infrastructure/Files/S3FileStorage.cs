using Amazon.S3;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3.Model;
using Amazon.S3.Util;

using ECM.File.Application.Files;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECM.File.Infrastructure.Files;

internal sealed class S3FileStorage(IAmazonS3 client, IOptions<FileStorageOptions> options, ILogger<S3FileStorage> logger) : IFileStorage
{
    private readonly IAmazonS3 _client = client;
    private readonly FileStorageOptions _options = options.Value;
    private readonly ILogger<S3FileStorage> _logger = logger;

    public async Task UploadAsync(
        string storageKey,
        Stream content,
        string contentType,
        string? originalFileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        await EnsureBucketExistsAsync(cancellationToken);

        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = storageKey,
            InputStream = content,
            ContentType = contentType,
        };

        var normalizedFileName = NormalizeFileName(originalFileName);
        if (!string.IsNullOrWhiteSpace(normalizedFileName))
        {
            request.Metadata["x-amz-meta-original-filename"] = normalizedFileName;
        }

        if (content.CanSeek)
        {
            request.Headers.ContentLength = content.Length - content.Position;
        }

        await _client.PutObjectAsync(request, cancellationToken);
    }

    public Task<Uri?> GetDownloadLinkAsync(string storageKey, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return Task.FromResult<Uri?>(null);
        }

        var expiresAt = DateTime.UtcNow.Add(lifetime);
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = storageKey,
            Expires = expiresAt,
        };

        var url = _client.GetPreSignedURL(request);
        return Task.FromResult(Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri : null);
    }

    public Task<FileDownload?> DownloadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        return DownloadObjectAsync(storageKey, cancellationToken);
    }

    public Task<FileDownload?> DownloadThumbnailAsync(string storageKey, int width, int height, string fit, CancellationToken cancellationToken = default)
    {
        var normalizedFit = string.IsNullOrWhiteSpace(fit) ? "cover" : fit.Trim().ToLowerInvariant();
        var thumbnailKey = $"thumbnails/{width}x{height}/{normalizedFit}/{storageKey}";
        return DownloadObjectAsync(thumbnailKey, cancellationToken);
    }

    private async Task<FileDownload?> DownloadObjectAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key,
            }, cancellationToken);

            await using var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);

            var contentType = string.IsNullOrWhiteSpace(response.Headers.ContentType)
                ? "application/octet-stream"
                : response.Headers.ContentType;

            var fileName = response.Metadata.Keys.Contains("x-amz-meta-original-filename", StringComparer.OrdinalIgnoreCase)
                ? response.Metadata["x-amz-meta-original-filename"]
                : null;

            var lastModified = response.LastModified == default
                ? (DateTimeOffset?)null
                : response.LastModified;

            return new FileDownload(memoryStream.ToArray(), contentType, fileName, lastModified);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound || string.Equals(exception.ErrorCode, "NoSuchKey", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug(exception, "Object {Key} not found in bucket {Bucket}.", key, _options.BucketName);
            return null;
        }
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (await BucketExistsAsync(cancellationToken))
            {
                return;
            }

            await CreateBucketAsync(cancellationToken);
        }
        catch (AmazonS3Exception exception) when (exception.ErrorCode == "BucketAlreadyOwnedByYou" ||
                                                 exception.StatusCode == HttpStatusCode.Conflict)
        {
            _logger.LogDebug("Bucket {Bucket} already exists.", _options.BucketName);
        }
    }

    private async Task<bool> BucketExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(_options.ServiceUrl))
            {
                try
                {
                    var response = await _client.ListBucketsAsync(new ListBucketsRequest(), cancellationToken);
                    return response.Buckets.Any(bucket => string.Equals(bucket.BucketName, _options.BucketName, StringComparison.Ordinal));
                }
                catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.Forbidden ||
                                                          string.Equals(exception.ErrorCode, "AccessDenied", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug(exception, "Access denied when listing buckets for custom endpoint {ServiceUrl}. Falling back to direct bucket existence check.", _options.ServiceUrl);
                }
            }

            return await AmazonS3Util.DoesS3BucketExistV2Async(_client, _options.BucketName);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound ||
                                                  string.Equals(exception.ErrorCode, "NoSuchBucket", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
    }

    private Task<PutBucketResponse> CreateBucketAsync(CancellationToken cancellationToken)
    {
        return _client.PutBucketAsync(new PutBucketRequest
        {
            BucketName = _options.BucketName,
            BucketRegionName = _options.Region,
            UseClientRegion = true,
        }, cancellationToken);
    }

    private static string? NormalizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var trimmed = fileName.Trim();
        var normalized = Path.GetFileName(trimmed);

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
