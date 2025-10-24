using ECM.Ocr.Application.Abstractions;
using ECM.Ocr.Application.Models;

namespace ECM.Ocr.Application.Queries;

public sealed class GetOcrSampleResultQueryHandler(IOcrProvider provider)
{
    private readonly IOcrProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));

    public Task<OcrResult> HandleAsync(GetOcrSampleResultQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return _provider.GetSampleResultAsync(query.SampleId, cancellationToken);
    }
}
