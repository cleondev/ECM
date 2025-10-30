using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ECM.File.Application.Files;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace ECM.File.Infrastructure.Files;

internal sealed class MinioFileStorage(IMinioClient client, IOptions<FileStorageOptions> options, ILogger<MinioFileStorage> logger) : IFileStorage
{
    private readonly IMinioClient _client = client;
    private readonly FileStorageOptions _options = options.Value;
    private readonly ILogger<MinioFileStorage> _logger = logger;

    public async Task UploadAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        _logger.LogDebug("Starting MinIO upload for {Key} with content type {ContentType}.", storageKey, contentType);

        var stopwatch = Stopwatch.StartNew();

        await EnsureBucketExistsAsync(cancellationToken);

        var (uploadStream, ownsStream) = await PrepareStreamAsync(content, cancellationToken);

        try
        {
            var objectSize = uploadStream.Length - uploadStream.Position;

            _logger.LogDebug(
                "Uploading object {Key} to bucket {Bucket}. Size: {Size} bytes. Position: {Position}.",
                storageKey,
                _options.BucketName,
                objectSize,
                uploadStream.Position);

            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(storageKey)
                .WithStreamData(uploadStream)
                .WithObjectSize(objectSize)
                .WithContentType(contentType);

            await _client.PutObjectAsync(putObjectArgs, cancellationToken);

            _logger.LogDebug(
                "Successfully uploaded object {Key} to bucket {Bucket} in {ElapsedMilliseconds} ms.",
                storageKey,
                _options.BucketName,
                stopwatch.ElapsedMilliseconds);
        }
        catch (MinioException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                exception,
                "MinIO returned an error while uploading object {Key} to bucket {Bucket} after {ElapsedMilliseconds} ms.",
                storageKey,
                _options.BucketName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                exception,
                "Unexpected error while uploading object {Key} to bucket {Bucket} after {ElapsedMilliseconds} ms.",
                storageKey,
                _options.BucketName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                exception,
                "Upload of object {Key} to bucket {Bucket} was cancelled after {ElapsedMilliseconds} ms.",
                storageKey,
                _options.BucketName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload object {Key} to bucket {Bucket}.", storageKey, _options.BucketName);
            throw;
        } 
        finally
        {
            stopwatch.Stop();

            if (ownsStream)
            {
                await uploadStream.DisposeAsync();
            }
        }
    }

    public async Task<Uri?> GetDownloadLinkAsync(string storageKey, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return null;
        }

        var expirySeconds = (int)Math.Ceiling(lifetime.TotalSeconds);
        expirySeconds = Math.Max(expirySeconds, 1);

        var args = new PresignedGetObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(storageKey)
            .WithExpiry(expirySeconds);

        var url = await _client.PresignedGetObjectAsync(args);
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri : null;
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
        await using var memoryStream = new MemoryStream();

        try
        {
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(key)
                .WithCallbackStream(stream =>
                {
                    stream.CopyTo(memoryStream);
                });

            await _client.GetObjectAsync(getObjectArgs, cancellationToken);

            var statArgs = new StatObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(key);

            var stat = await _client.StatObjectAsync(statArgs, cancellationToken);

            var contentType = string.IsNullOrWhiteSpace(stat.ContentType)
                ? "application/octet-stream"
                : stat.ContentType;

            string? fileName = null;
            if (stat.MetaData is { Count: > 0 } metadata)
            {
                const string fileNameKey = "x-amz-meta-original-filename";
                if (metadata.TryGetValue(fileNameKey, out var value))
                {
                    fileName = value;
                }
            }

            var lastModified = GetLastModified(stat.LastModified);

            return new FileDownload(memoryStream.ToArray(), contentType, fileName, lastModified);
        }
        catch (ObjectNotFoundException exception)
        {
            _logger.LogDebug(exception, "Object {Key} not found in bucket {Bucket}.", key, _options.BucketName);
            return null;
        }
        catch (BucketNotFoundException exception)
        {
            _logger.LogDebug(exception, "Bucket {Bucket} not found when accessing {Key}.", _options.BucketName, key);
            return null;
        }
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        var bucketExistsArgs = new BucketExistsArgs()
            .WithBucket(_options.BucketName);

        if (await _client.BucketExistsAsync(bucketExistsArgs, cancellationToken))
        {
            _logger.LogDebug("Bucket {Bucket} already exists when preparing for upload.", _options.BucketName);
            return;
        }

        var makeBucketArgs = new MakeBucketArgs()
            .WithBucket(_options.BucketName);

        try
        {
            _logger.LogInformation("Creating bucket {Bucket} for MinIO uploads.", _options.BucketName);
            await _client.MakeBucketAsync(makeBucketArgs, cancellationToken);
            _logger.LogInformation("Successfully created bucket {Bucket} for MinIO uploads.", _options.BucketName);
        }
        catch (MinioException exception) when (IsBucketAlreadyOwned(exception))
        {
            _logger.LogDebug(exception, "Bucket {Bucket} already exists.", _options.BucketName);
        }
    }

    private async Task<(Stream Stream, bool OwnsStream)> PrepareStreamAsync(Stream source, CancellationToken cancellationToken)
    {
        if (source.CanSeek)
        {
            _logger.LogDebug("Using seekable source stream for MinIO upload.");
            return (source, false);
        }

        var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        _logger.LogDebug("Buffered non-seekable stream for MinIO upload. Buffered size: {Size} bytes.", buffer.Length);
        return (buffer, true);
    }

    private static bool IsBucketAlreadyOwned(MinioException exception)
    {
        if (exception is BucketNotFoundException)
        {
            return false;
        }

        var message = exception.Message ?? exception.Response?.Message;
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        return message.Contains("already owned", StringComparison.OrdinalIgnoreCase)
            || message.Contains("already exists", StringComparison.OrdinalIgnoreCase);
    }

    private static DateTimeOffset? GetLastModified(object? value)
    {
        return value switch
        {
            DateTimeOffset dto when dto != default => dto,
            DateTime dt when dt != default => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
            _ => null,
        };
    }
}
