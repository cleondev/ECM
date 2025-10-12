using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.SearchIndexer.Application.Indexing.Abstractions;
using ECM.SearchIndexer.Domain.Indexing;

namespace ECM.SearchIndexer.Application.Indexing;

public sealed class EnqueueDocumentIndexingHandler(IIndexingJobScheduler scheduler)
{
    private readonly IIndexingJobScheduler _scheduler = scheduler;

    public async Task<EnqueueDocumentIndexingResult> HandleAsync(
        EnqueueDocumentIndexingCommand command,
        CancellationToken cancellationToken = default)
    {
        var metadata = command.Metadata is null
            ? null
            : new Dictionary<string, string>(command.Metadata);

        var document = SearchDocument.Create(
            command.DocumentId,
            command.Title,
            command.Summary,
            command.Content,
            command.Tags,
            metadata);

        var record = document.ToRecord();
        var jobId = await _scheduler.EnqueueAsync(record, cancellationToken).ConfigureAwait(false);

        return new EnqueueDocumentIndexingResult(jobId, record.DocumentId);
    }
}
