using ECM.Ocr.Application.Commands;

namespace ECM.Ocr.Application.Events;

public sealed class OcrProcessingEventProcessor(StartOcrCommandHandler handler)
{
    private readonly StartOcrCommandHandler _handler = handler ?? throw new ArgumentNullException(nameof(handler));

    public Task HandleDocumentUploadedAsync(DocumentUploadedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var command = new StartOcrCommand(
            integrationEvent.DocumentId,
            integrationEvent.Title,
            integrationEvent.Summary,
            integrationEvent.Content,
            integrationEvent.Metadata,
            integrationEvent.Tags);

        return _handler.HandleAsync(command, cancellationToken);
    }
}
