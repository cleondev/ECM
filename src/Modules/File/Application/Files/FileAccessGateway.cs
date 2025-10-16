using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.Abstractions.Files;
using ECM.BuildingBlocks.Application;

namespace ECM.File.Application.Files;

public sealed class FileAccessGateway(IFileStorage storage) : IFileAccessGateway
{
    private readonly IFileStorage _storage = storage;

    public async Task<OperationResult<FileDownloadLink>> GetDownloadLinkAsync(
        string storageKey,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return OperationResult<FileDownloadLink>.Failure("StorageKeyRequired");
        }

        if (lifetime <= TimeSpan.Zero)
        {
            return OperationResult<FileDownloadLink>.Failure("LifetimeMustBePositive");
        }

        var uri = await _storage.GetDownloadLinkAsync(storageKey, lifetime, cancellationToken);
        if (uri is null)
        {
            return OperationResult<FileDownloadLink>.Failure("NotFound");
        }

        var expiresAt = DateTimeOffset.UtcNow.Add(lifetime);
        return OperationResult<FileDownloadLink>.Success(new FileDownloadLink(uri, expiresAt));
    }

    public async Task<OperationResult<FileContent>> GetContentAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return OperationResult<FileContent>.Failure("StorageKeyRequired");
        }

        var download = await _storage.DownloadAsync(storageKey, cancellationToken);
        if (download is null)
        {
            return OperationResult<FileContent>.Failure("NotFound");
        }

        var contentType = string.IsNullOrWhiteSpace(download.ContentType)
            ? "application/octet-stream"
            : download.ContentType;

        return OperationResult<FileContent>.Success(
            new FileContent(download.Content, contentType, download.FileName, download.LastModifiedUtc));
    }

    public async Task<OperationResult<FileContent>> GetThumbnailAsync(
        string storageKey,
        int width,
        int height,
        string fit,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return OperationResult<FileContent>.Failure("StorageKeyRequired");
        }

        if (width <= 0 || height <= 0)
        {
            return OperationResult<FileContent>.Failure("InvalidDimensions");
        }

        var normalizedFit = string.IsNullOrWhiteSpace(fit)
            ? "cover"
            : fit.Trim().ToLowerInvariant();

        var download = await _storage.DownloadThumbnailAsync(storageKey, width, height, normalizedFit, cancellationToken);
        if (download is null)
        {
            return OperationResult<FileContent>.Failure("NotFound");
        }

        var contentType = string.IsNullOrWhiteSpace(download.ContentType)
            ? "image/jpeg"
            : download.ContentType;

        return OperationResult<FileContent>.Success(
            new FileContent(download.Content, contentType, download.FileName, download.LastModifiedUtc));
    }
}
