using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.File.Domain.Files;

namespace ECM.File.Application.Files;

public sealed class GetRecentFilesQueryHandler(IFileRepository repository)
{
    private readonly IFileRepository _repository = repository;

    public Task<IReadOnlyCollection<StoredFile>> HandleAsync(GetRecentFilesQuery query, CancellationToken cancellationToken)
        => _repository.GetRecentAsync(query.Limit, cancellationToken);
}
