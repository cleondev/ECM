using System;
using System.Threading;

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;

using FileServices.Configuration;
using FileServices.Services.Models;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileServices.Services;

public sealed class MinioFileStorageService : IFileStorageService, IDisposable
{
    private readonly IAmazonS3 _s3Client;
    private readonly MinioOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MinioFileStorageService> _logger;
    private readonly SemaphoreSlim _bucketInitializationGate = new(1, 1);

    private bool _bucketInitialized;

    public MinioFileStorageService(
        IAmazonS3 s3Client,
        IOptions<MinioOptions> options,
        TimeProvider timeProvider,
        ILogger<MinioFileStorageService> logger)
    {
        _s3Client = s3Client;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<PresignedUploadResult> GenerateUploadUrlAsync(string objectKey, string contentType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            throw new ArgumentException("The object key must be provided.", nameof(objectKey));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("The content type must be provided.", nameof(contentType));
        }

        await EnsureBucketExistsAsync(cancellationToken).ConfigureAwait(false);

        var expiresIn = TimeSpan.FromMinutes(_options.UploadUrlExpiryMinutes);
        var expiration = _timeProvider.GetUtcNow().Add(expiresIn);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.Bucket,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            ContentType = contentType,
            Expires = expiration.UtcDateTime,
        };

        var url = _s3Client.GetPreSignedURL(request);

        _logger.LogInformation("Generated presigned upload URL for bucket {Bucket} and key {Key}", _options.Bucket, objectKey);

        return new PresignedUploadResult(objectKey, new Uri(url), expiresIn);
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        if (_bucketInitialized)
        {
            return;
        }

        await _bucketInitializationGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_bucketInitialized)
            {
                return;
            }

            var bucketExists = await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, _options.Bucket).ConfigureAwait(false);
            if (!bucketExists)
            {
                _logger.LogInformation("Bucket {Bucket} was not found. Creating it now.", _options.Bucket);
                await _s3Client.PutBucketAsync(new PutBucketRequest
                {
                    BucketName = _options.Bucket,
                    UseClientRegion = true,
                }, cancellationToken).ConfigureAwait(false);
            }

            _bucketInitialized = true;
        }
        finally
        {
            _bucketInitializationGate.Release();
        }
    }

    public void Dispose()
    {
        _bucketInitializationGate.Dispose();
        GC.SuppressFinalize(this);
    }
}
