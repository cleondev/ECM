using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.SearchIndexer.Application.Indexing;
using ECM.SearchIndexer.Domain.Indexing;

namespace ECM.SearchIndexer.Application.Events;

public sealed class SearchIndexingEventProcessor(EnqueueDocumentIndexingHandler handler)
{
    private readonly EnqueueDocumentIndexingHandler _handler = handler ?? throw new ArgumentNullException(nameof(handler));

    public Task<EnqueueDocumentIndexingResult> HandleDocumentUploadedAsync(
        DocumentUploadedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var command = new EnqueueDocumentIndexingCommand(
            integrationEvent.DocumentId,
            integrationEvent.Title,
            integrationEvent.Summary,
            integrationEvent.Content,
            integrationEvent.Metadata,
            integrationEvent.Tags,
            SearchIndexingType.Basic);

        return _handler.HandleAsync(command, cancellationToken);
    }

    public Task<EnqueueDocumentIndexingResult> HandleOcrCompletedAsync(
        OcrCompletedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var command = new EnqueueDocumentIndexingCommand(
            integrationEvent.DocumentId,
            integrationEvent.Title,
            integrationEvent.Summary,
            integrationEvent.Content,
            integrationEvent.Metadata,
            integrationEvent.Tags,
            SearchIndexingType.Advanced);

        return _handler.HandleAsync(command, cancellationToken);
    }
}
