using System.Text.Json;
using ECM.Ocr.Domain.Results;
using ECM.Ocr.Domain.Templates;

namespace ECM.Ocr.Domain.Annotations;

public sealed class OcrAnnotation
{
    private OcrAnnotation()
    {
    }

    public Guid Id { get; private set; }

    public Guid DocumentId { get; private set; }

    public Guid VersionId { get; private set; }

    public Guid? TemplateId { get; private set; }

    public string? FieldKey { get; private set; }

    public string? ValueText { get; private set; }

    public JsonDocument? BoundingBox { get; private set; }

    public decimal? Confidence { get; private set; }

    public string? Source { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public OcrTemplate? Template { get; private set; }

    public OcrDocumentResult Result { get; private set; } = null!;

    public void SetValue(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        ValueText = value;
    }
}
