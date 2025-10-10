using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ECM.File.Domain.Files;

public interface IFileRepository
{
    Task AddAsync(FileEntry entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<FileEntry>> GetRecentAsync(int limit, CancellationToken cancellationToken = default);
}
