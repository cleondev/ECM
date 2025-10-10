namespace ECM.Document.Domain.Tags;

public sealed class TagNamespace
{
    private readonly List<TagLabel> _labels = [];

    private TagNamespace()
    {
        NamespaceSlug = null!;
        Kind = null!;
    }

    public TagNamespace(string namespaceSlug, string kind, Guid? ownerUserId, string? displayName, DateTimeOffset createdAtUtc)
        : this()
    {
        if (string.IsNullOrWhiteSpace(namespaceSlug))
        {
            throw new ArgumentException("Namespace slug is required.", nameof(namespaceSlug));
        }

        if (string.IsNullOrWhiteSpace(kind))
        {
            throw new ArgumentException("Namespace kind is required.", nameof(kind));
        }

        var normalizedKind = kind.Trim();
        if (!string.Equals(normalizedKind, "system", StringComparison.Ordinal)
            && !string.Equals(normalizedKind, "user", StringComparison.Ordinal))
        {
            throw new ArgumentException("Namespace kind must be either 'system' or 'user'.", nameof(kind));
        }

        NamespaceSlug = namespaceSlug.Trim();
        Kind = normalizedKind;
        OwnerUserId = ownerUserId;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        CreatedAtUtc = createdAtUtc;
    }

    public string NamespaceSlug { get; private set; }

    public string Kind { get; private set; }

    public Guid? OwnerUserId { get; private set; }

    public string? DisplayName { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<TagLabel> Labels => _labels.AsReadOnly();
}
