using System.Text.Json;
using ECM.Ocr.Domain.Results;

namespace ECM.Ocr.Domain.Extractions;

public sealed class OcrExtraction
{
    private OcrExtraction()
    {
    }

    public Guid DocumentId { get; private set; }

    public Guid VersionId { get; private set; }

    public string FieldKey { get; private set; } = string.Empty;

    public string? ValueText { get; private set; }

    public decimal? Confidence { get; private set; }

    public JsonDocument? Provenance { get; private set; }

    public OcrDocumentResult Result { get; private set; } = null!;

    public void SetValue(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        ValueText = value;
    }
}
