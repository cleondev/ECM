using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ECM.File.Application.Files;

public interface IFileStorage
{
    Task UploadAsync(
        string storageKey,
        Stream content,
        string contentType,
        string? originalFileName,
        CancellationToken cancellationToken = default);

    Task<Uri?> GetDownloadLinkAsync(string storageKey, TimeSpan lifetime, CancellationToken cancellationToken = default);

    Task<FileDownload?> DownloadAsync(string storageKey, CancellationToken cancellationToken = default);

    Task<FileDownload?> DownloadThumbnailAsync(string storageKey, int width, int height, string fit, CancellationToken cancellationToken = default);
}
