using ECM.Document.Domain.Documents;

namespace ECM.Document.Domain.Tags;

public sealed class DocumentTag
{
    private DocumentTag()
    {
    }

    public DocumentTag(DocumentId documentId, Guid tagId, Guid? appliedBy, DateTimeOffset appliedAtUtc)
    {
        if (documentId == default)
        {
            throw new ArgumentException("Document identifier is required.", nameof(documentId));
        }

        if (tagId == Guid.Empty)
        {
            throw new ArgumentException("Tag identifier is required.", nameof(tagId));
        }

        DocumentId = documentId;
        TagId = tagId;
        AppliedBy = appliedBy;
        AppliedAtUtc = appliedAtUtc;
    }

    public DocumentId DocumentId { get; private set; }

    public Document? Document { get; private set; }

    public Guid TagId { get; private set; }

    public TagLabel? Tag { get; private set; }

    public Guid? AppliedBy { get; private set; }

    public DateTimeOffset AppliedAtUtc { get; private set; }
}
