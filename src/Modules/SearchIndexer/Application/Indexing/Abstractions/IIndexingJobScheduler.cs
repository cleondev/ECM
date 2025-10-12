using System.Threading;
using System.Threading.Tasks;
using ECM.SearchIndexer.Domain.Indexing;

namespace ECM.SearchIndexer.Application.Indexing.Abstractions;

public interface IIndexingJobScheduler
{
    Task<string> EnqueueAsync(SearchIndexRecord record, CancellationToken cancellationToken = default);
}
