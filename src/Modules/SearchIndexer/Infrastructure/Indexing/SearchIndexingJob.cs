using System.Threading;
using System.Threading.Tasks;
using ECM.SearchIndexer.Application.Indexing.Abstractions;
using ECM.SearchIndexer.Domain.Indexing;
using Microsoft.Extensions.Logging;

namespace ECM.SearchIndexer.Infrastructure.Indexing;

internal sealed class SearchIndexingJob(ISearchIndexWriter writer, ILogger<SearchIndexingJob> logger)
{
    private readonly ISearchIndexWriter _writer = writer;
    private readonly ILogger<SearchIndexingJob> _logger = logger;

    public async Task ExecuteAsync(SearchIndexRecord record, CancellationToken cancellationToken = default)
    {
        await _writer.UpsertAsync(record, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Indexed document {DocumentId} into search store", record.DocumentId);
    }
}
