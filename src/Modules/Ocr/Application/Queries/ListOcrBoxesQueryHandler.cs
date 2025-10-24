using ECM.Ocr.Application.Abstractions;
using ECM.Ocr.Application.Models;

namespace ECM.Ocr.Application.Queries;

public sealed class ListOcrBoxesQueryHandler(IOcrProvider provider)
{
    private readonly IOcrProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));

    public Task<OcrBoxesResult> HandleAsync(ListOcrBoxesQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return _provider.ListBoxesAsync(query.SampleId, cancellationToken);
    }
}
