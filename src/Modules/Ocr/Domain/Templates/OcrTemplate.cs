namespace ECM.Ocr.Domain.Templates;

public sealed class OcrTemplate
{
    private readonly List<OcrFieldDefinition> _fields = [];
    private readonly List<Annotations.OcrAnnotation> _annotations = [];

    private OcrTemplate()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public int Version { get; private set; }

    public string? PageSide { get; private set; }

    public string? SizeRatio { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<OcrFieldDefinition> Fields => _fields;

    public IReadOnlyCollection<Annotations.OcrAnnotation> Annotations => _annotations;
}
