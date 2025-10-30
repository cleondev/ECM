using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;

namespace ECM.Abstractions.Files;

public interface IFileAccessGateway
{
    Task<OperationResult<FileDownloadLink>> GetDownloadLinkAsync(
        string storageKey,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default);

    Task<OperationResult<FileContent>> GetContentAsync(
        string storageKey,
        CancellationToken cancellationToken = default);

    Task<OperationResult<FileContent>> GetThumbnailAsync(
        string storageKey,
        int width,
        int height,
        string fit,
        CancellationToken cancellationToken = default);
}
