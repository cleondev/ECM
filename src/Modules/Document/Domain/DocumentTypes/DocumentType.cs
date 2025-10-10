using ECM.Document.Domain.Documents;
using DocumentAggregate = ECM.Document.Domain.Documents.Document;

namespace ECM.Document.Domain.DocumentTypes;

public sealed class DocumentType
{
    private readonly List<DocumentAggregate> _documents = [];

    private DocumentType()
    {
        TypeKey = null!;
        TypeName = null!;
    }

    public DocumentType(Guid id, string typeKey, string typeName, bool isActive, DateTimeOffset createdAtUtc)
        : this()
    {
        if (string.IsNullOrWhiteSpace(typeKey))
        {
            throw new ArgumentException("Type key is required.", nameof(typeKey));
        }

        if (string.IsNullOrWhiteSpace(typeName))
        {
            throw new ArgumentException("Type name is required.", nameof(typeName));
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        TypeKey = typeKey.Trim();
        TypeName = typeName.Trim();
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public string TypeKey { get; private set; }

    public string TypeName { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<DocumentAggregate> Documents => _documents.AsReadOnly();
}
