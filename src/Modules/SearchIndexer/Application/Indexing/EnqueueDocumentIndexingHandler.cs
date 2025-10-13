using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.SearchIndexer.Application.Indexing.Abstractions;
using ECM.SearchIndexer.Domain.Indexing;
using Microsoft.Extensions.Logging;

namespace ECM.SearchIndexer.Application.Indexing;

public sealed class EnqueueDocumentIndexingHandler(
    IIndexingJobScheduler scheduler,
    ILogger<EnqueueDocumentIndexingHandler> logger)
{
    private readonly IIndexingJobScheduler _scheduler = scheduler;
    private readonly ILogger<EnqueueDocumentIndexingHandler> _logger = logger;

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

        var record = document.ToRecord(command.IndexingType);
        var jobId = await _scheduler.EnqueueAsync(record, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Scheduled search indexing job {JobId} for document {DocumentId} ({Title}).",
            jobId,
            record.DocumentId,
            record.Title);

        return new EnqueueDocumentIndexingResult(jobId, record.DocumentId);
    }
}
