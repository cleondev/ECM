using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.File.Domain.Files;

namespace ECM.File.Application.Files;

public interface IFileRepository
{
    Task<StoredFile> AddAsync(StoredFile file, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<StoredFile>> GetRecentAsync(int limit, CancellationToken cancellationToken = default);
}
