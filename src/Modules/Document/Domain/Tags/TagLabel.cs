using ECM.BuildingBlocks.Domain.Events;
using ECM.Document.Domain.Documents;
using ECM.Document.Domain.Tags.Events;

namespace ECM.Document.Domain.Tags;

public sealed class TagLabel : IHasDomainEvents
{
    private readonly List<DocumentTag> _documentTags = [];
    private readonly List<IDomainEvent> _domainEvents = [];

    private TagLabel()
    {
        NamespaceSlug = null!;
        Slug = null!;
        Path = null!;
    }

    private TagLabel(
        Guid id,
        string namespaceSlug,
        string slug,
        string path,
        bool isActive,
        Guid? createdBy,
        DateTimeOffset createdAtUtc)
        : this()
    {
        if (string.IsNullOrWhiteSpace(namespaceSlug))
        {
            throw new ArgumentException("Namespace slug is required.", nameof(namespaceSlug));
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Slug is required.", nameof(slug));
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        NamespaceSlug = namespaceSlug.Trim();
        Slug = slug.Trim();
        Path = path.Trim();
        IsActive = isActive;
        CreatedBy = createdBy;
        CreatedAtUtc = createdAtUtc;
    }

    public static TagLabel Create(
        string namespaceSlug,
        string slug,
        string path,
        Guid? createdBy,
        DateTimeOffset createdAtUtc)
    {
        var tagLabel = new TagLabel(Guid.NewGuid(), namespaceSlug, slug, path, isActive: true, createdBy, createdAtUtc);

        tagLabel.Raise(new TagLabelCreatedDomainEvent(
            tagLabel.Id,
            tagLabel.NamespaceSlug,
            tagLabel.Path,
            tagLabel.CreatedBy,
            tagLabel.CreatedAtUtc));

        return tagLabel;
    }

    public Guid Id { get; private set; }

    public string NamespaceSlug { get; private set; }

    public TagNamespace? Namespace { get; private set; }

    public string Slug { get; private set; }

    public string Path { get; private set; }

    public bool IsActive { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<DocumentTag> DocumentTags => _documentTags.AsReadOnly();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void MarkDeleted(DateTimeOffset deletedAtUtc)
    {
        Raise(new TagLabelDeletedDomainEvent(Id, NamespaceSlug, Path, deletedAtUtc));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    private void Raise(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }
}
