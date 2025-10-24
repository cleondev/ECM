using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            NormalizeMetadata(integrationEvent.Metadata),
            integrationEvent.Tags);

        return _handler.HandleAsync(command, cancellationToken);
    }

    private static IReadOnlyDictionary<string, string>? NormalizeMetadata(IDictionary<string, string>? metadata)
    {
        return metadata switch
        {
            null => null,
            IReadOnlyDictionary<string, string> readOnly => readOnly,
            _ => new ReadOnlyDictionary<string, string>(metadata)
        };
    }
}
