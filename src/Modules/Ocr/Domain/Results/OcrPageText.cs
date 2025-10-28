namespace ECM.Ocr.Domain.Results;

public sealed class OcrPageText
{
    private OcrPageText()
    {
    }

    public Guid DocumentId { get; private set; }

    public Guid VersionId { get; private set; }

    public int PageNo { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public OcrDocumentResult Result { get; private set; } = null!;
}
