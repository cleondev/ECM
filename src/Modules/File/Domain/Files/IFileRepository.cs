using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ECM.File.Domain.Files;

public interface IFileRepository
{
    Task<StoredFile> AddAsync(StoredFile file, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<StoredFile>> GetRecentAsync(int limit, CancellationToken cancellationToken = default);
}
