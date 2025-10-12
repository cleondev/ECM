using System.Threading;
using System.Threading.Tasks;
using ECM.SearchIndexer.Domain.Indexing;

namespace ECM.SearchIndexer.Application.Indexing.Abstractions;

public interface ISearchIndexWriter
{
    Task UpsertAsync(SearchIndexRecord record, CancellationToken cancellationToken = default);
}
