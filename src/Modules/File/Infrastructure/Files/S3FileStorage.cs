using Amazon.S3;
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

    public async Task UploadAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken = default)
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

        if (content.CanSeek)
        {
            request.Headers.ContentLength = content.Length - content.Position;
        }

        await _client.PutObjectAsync(request, cancellationToken);
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await AmazonS3Util.DoesS3BucketExistV2Async(_client, _options.BucketName);
            if (!response)
            {
                await _client.PutBucketAsync(new PutBucketRequest
                {
                    BucketName = _options.BucketName,
                    UseClientRegion = true,
                }, cancellationToken);
            }
        }
        catch (AmazonS3Exception exception) when (exception.ErrorCode == "BucketAlreadyOwnedByYou")
        {
            _logger.LogDebug("Bucket {Bucket} already exists.", _options.BucketName);
        }
    }
}
