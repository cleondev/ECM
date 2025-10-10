using ECM.Document.Domain.Documents;

namespace ECM.Document.Domain.Tags;

public sealed class TagLabel
{
    private readonly List<DocumentTag> _documentTags = [];

    private TagLabel()
    {
        NamespaceSlug = null!;
        Slug = null!;
        Path = null!;
    }

    public TagLabel(
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

    public Guid Id { get; private set; }

    public string NamespaceSlug { get; private set; }

    public TagNamespace? Namespace { get; private set; }

    public string Slug { get; private set; }

    public string Path { get; private set; }

    public bool IsActive { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<DocumentTag> DocumentTags => _documentTags.AsReadOnly();
}
