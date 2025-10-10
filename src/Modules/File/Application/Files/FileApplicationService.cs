using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.File.Domain.Files;

namespace ECM.File.Application.Files;

public sealed class FileApplicationService(IFileRepository repository)
{
    private readonly IFileRepository _repository = repository;

    public async Task<OperationResult<FileEntry>> RegisterAsync(RegisterFileCommand command, CancellationToken cancellationToken)
    {
        var result = command.ToDomain();
        if (result.IsFailure || result.Value is null)
        {
            return result;
        }

        await _repository.AddAsync(result.Value, cancellationToken);
        return result;
    }

    public Task<IReadOnlyCollection<FileEntry>> GetRecentAsync(int limit, CancellationToken cancellationToken)
        => _repository.GetRecentAsync(limit, cancellationToken);
}
