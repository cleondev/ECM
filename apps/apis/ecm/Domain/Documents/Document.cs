namespace Ecm.Domain.Documents;

public sealed class Document
{
    private Document(DocumentId id, DocumentTitle title, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Title = title;
        CreatedAtUtc = createdAtUtc;
    }

    public DocumentId Id { get; }

    public DocumentTitle Title { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public static Document Create(DocumentTitle title, DateTimeOffset createdAtUtc)
    {
        return new Document(DocumentId.New(), title, createdAtUtc);
    }
}
