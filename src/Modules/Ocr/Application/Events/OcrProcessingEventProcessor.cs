using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using ECM.Ocr.Application.Abstractions;
using ECM.Ocr.Application.Commands;
using Microsoft.Extensions.Logging;

namespace ECM.Ocr.Application.Events;

public sealed class OcrProcessingEventProcessor(
    StartOcrCommandHandler handler,
    IDocumentFileLinkService fileLinkService,
    ILogger<OcrProcessingEventProcessor> logger)
{
    private readonly StartOcrCommandHandler _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    private readonly IDocumentFileLinkService _fileLinkService = fileLinkService
        ?? throw new ArgumentNullException(nameof(fileLinkService));
    private readonly ILogger<OcrProcessingEventProcessor> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    public async Task HandleDocumentUploadedAsync(DocumentUploadedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var fileUrl = await _fileLinkService.GetDownloadLinkAsync(integrationEvent.DocumentId, cancellationToken)
            .ConfigureAwait(false);

        if (fileUrl is null)
        {
            _logger.LogWarning(
                "Skipping OCR trigger because no downloadable file link was found for document {DocumentId}.",
                integrationEvent.DocumentId);
            return;
        }

        var command = new StartOcrCommand(
            integrationEvent.DocumentId,
            integrationEvent.Title,
            integrationEvent.Summary,
            integrationEvent.Content,
            NormalizeMetadata(integrationEvent.Metadata),
            integrationEvent.Tags,
            fileUrl);

        await _handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);
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
