using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.SearchIndexer.Domain.Indexing;

namespace ECM.SearchIndexer.Application.Indexing.Abstractions;

public interface ISearchIndexReader
{
    Task<SearchIndexRecord?> FindByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SearchIndexRecord>> ListAsync(CancellationToken cancellationToken = default);
}
