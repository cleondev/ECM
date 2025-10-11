using System.Text.Json;

namespace ECM.Document.Domain.Documents;

public sealed class DocumentMetadata
{
    private DocumentMetadata()
    {
        Data = null!;
    }

    public DocumentMetadata(DocumentId documentId, JsonDocument data)
        : this()
    {
        DocumentId = documentId;
        Data = data;
    }

    public DocumentId DocumentId { get; private set; }

    public Document? Document { get; private set; }

    public JsonDocument Data { get; private set; }
}
