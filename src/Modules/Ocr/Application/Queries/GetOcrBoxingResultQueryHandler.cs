using ECM.Ocr.Application.Abstractions;
using ECM.Ocr.Application.Models;

namespace ECM.Ocr.Application.Queries;

public sealed class GetOcrBoxingResultQueryHandler(IOcrProvider provider)
{
    private readonly IOcrProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));

    public Task<OcrResult> HandleAsync(GetOcrBoxingResultQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return _provider.GetBoxingResultAsync(query.SampleId, query.BoxingId, cancellationToken);
    }
}
