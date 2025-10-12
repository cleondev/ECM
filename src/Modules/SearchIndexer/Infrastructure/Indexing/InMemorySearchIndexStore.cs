using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.SearchIndexer.Application.Indexing.Abstractions;
using ECM.SearchIndexer.Domain.Indexing;

namespace ECM.SearchIndexer.Infrastructure.Indexing;

internal sealed class InMemorySearchIndexStore : ISearchIndexWriter, ISearchIndexReader
{
    private readonly ConcurrentDictionary<Guid, SearchIndexRecord> _records = new();

    public Task UpsertAsync(SearchIndexRecord record, CancellationToken cancellationToken = default)
    {
        _records.AddOrUpdate(record.DocumentId, record, (_, _) => record);
        return Task.CompletedTask;
    }

    public Task<SearchIndexRecord?> FindByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        _records.TryGetValue(documentId, out var record);
        return Task.FromResult(record);
    }

    public Task<IReadOnlyCollection<SearchIndexRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<SearchIndexRecord> snapshot = _records.Values.ToArray();
        return Task.FromResult(snapshot);
    }
}
