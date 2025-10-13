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
        DocumentUploadedIntegrationEvent @event,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var command = new EnqueueDocumentIndexingCommand(
            @event.DocumentId,
            @event.Title,
            @event.Summary,
            @event.Content,
            @event.Metadata,
            @event.Tags,
            SearchIndexingType.Basic);

        return _handler.HandleAsync(command, cancellationToken);
    }

    public Task<EnqueueDocumentIndexingResult> HandleOcrCompletedAsync(
        OcrCompletedIntegrationEvent @event,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var command = new EnqueueDocumentIndexingCommand(
            @event.DocumentId,
            @event.Title,
            @event.Summary,
            @event.Content,
            @event.Metadata,
            @event.Tags,
            SearchIndexingType.Advanced);

        return _handler.HandleAsync(command, cancellationToken);
    }
}
