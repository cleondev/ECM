using FileServices.Services.Models;

namespace FileServices.Services;

public interface IFileStorageService
{
    Task<PresignedUploadResult> GenerateUploadUrlAsync(string objectKey, string contentType, CancellationToken cancellationToken = default);
}
