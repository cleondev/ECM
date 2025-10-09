using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.Modules.File.Domain.Files;

namespace ECM.Modules.File.Infrastructure.Files;

internal sealed class InMemoryFileRepository : IFileRepository
{
    private readonly ConcurrentDictionary<Guid, FileEntry> _storage = new();

    public Task AddAsync(FileEntry entry, CancellationToken cancellationToken = default)
    {
        _storage[entry.Id.Value] = entry;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<FileEntry>> GetRecentAsync(int limit, CancellationToken cancellationToken = default)
    {
        var result = _storage.Values
            .OrderByDescending(file => file.CreatedAtUtc)
            .Take(limit)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<FileEntry>>(result);
    }
}
