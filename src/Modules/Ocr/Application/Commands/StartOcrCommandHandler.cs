using ECM.Ocr.Application.Abstractions;
using ECM.Ocr.Application.Models;
using Microsoft.Extensions.Logging;

namespace ECM.Ocr.Application.Commands;

public sealed class StartOcrCommandHandler(IOcrProvider provider, ILogger<StartOcrCommandHandler> logger)
{
    private readonly IOcrProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    private readonly ILogger<StartOcrCommandHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<StartOcrResult> HandleAsync(StartOcrCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var result = await _provider.StartProcessingAsync(command, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(result.SampleId))
        {
            _logger.LogInformation(
                "Triggered OCR processing for document {DocumentId}.",
                command.DocumentId);
        }
        else
        {
            _logger.LogInformation(
                "Triggered OCR processing for document {DocumentId} with sample {SampleId}.",
                command.DocumentId,
                result.SampleId);
        }

        return result;
    }
}
