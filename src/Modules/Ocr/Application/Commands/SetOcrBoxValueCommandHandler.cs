using ECM.Ocr.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace ECM.Ocr.Application.Commands;

public sealed class SetOcrBoxValueCommandHandler(IOcrProvider provider, ILogger<SetOcrBoxValueCommandHandler> logger)
{
    private readonly IOcrProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    private readonly ILogger<SetOcrBoxValueCommandHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task HandleAsync(SetOcrBoxValueCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _provider.SetBoxValueAsync(command.SampleId, command.BoxId, command.Value, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Updated OCR box {BoxId} for sample {SampleId}.",
            command.BoxId,
            command.SampleId);
    }
}
