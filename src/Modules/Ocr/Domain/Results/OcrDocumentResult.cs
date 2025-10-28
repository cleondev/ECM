using ECM.Ocr.Domain.Annotations;
using ECM.Ocr.Domain.Extractions;

namespace ECM.Ocr.Domain.Results;

public sealed class OcrDocumentResult
{
    private readonly List<OcrPageText> _pageTexts = [];
    private readonly List<OcrExtraction> _extractions = [];
    private readonly List<OcrAnnotation> _annotations = [];

    private OcrDocumentResult()
    {
    }

    public Guid DocumentId { get; private set; }

    public Guid VersionId { get; private set; }

    public int Pages { get; private set; }

    public string? Lang { get; private set; }

    public string? Summary { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<OcrPageText> PageTexts => _pageTexts;

    public IReadOnlyCollection<OcrExtraction> Extractions => _extractions;

    public IReadOnlyCollection<OcrAnnotation> Annotations => _annotations;
}
