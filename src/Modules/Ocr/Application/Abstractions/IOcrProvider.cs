using ECM.Ocr.Application.Commands;
using ECM.Ocr.Application.Models;

namespace ECM.Ocr.Application.Abstractions;

public interface IOcrProvider
{
    Task<StartOcrResult> StartProcessingAsync(StartOcrCommand command, CancellationToken cancellationToken = default);

    Task<OcrResult> GetSampleResultAsync(string sampleId, CancellationToken cancellationToken = default);

    Task<OcrResult> GetBoxingResultAsync(string sampleId, string boxingId, CancellationToken cancellationToken = default);

    Task<OcrBoxesResult> ListBoxesAsync(string sampleId, CancellationToken cancellationToken = default);

    Task SetBoxValueAsync(string sampleId, string boxId, string value, CancellationToken cancellationToken = default);
}
