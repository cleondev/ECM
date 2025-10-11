using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;

namespace ECM.Abstractions.Files;

public interface IFileStorageGateway
{
    Task<OperationResult<FileUploadResult>> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken = default);
}
