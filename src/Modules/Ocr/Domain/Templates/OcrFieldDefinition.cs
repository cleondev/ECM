using System.Text.Json;

namespace ECM.Ocr.Domain.Templates;

public sealed class OcrFieldDefinition
{
    private OcrFieldDefinition()
    {
    }

    public Guid Id { get; private set; }

    public Guid TemplateId { get; private set; }

    public string FieldKey { get; private set; } = string.Empty;

    public JsonDocument? BoundingBoxRelative { get; private set; }

    public JsonDocument? Anchor { get; private set; }

    public string? Validator { get; private set; }

    public bool Required { get; private set; }

    public int? OrderNo { get; private set; }

    public OcrTemplate Template { get; private set; } = null!;
}
