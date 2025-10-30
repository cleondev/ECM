using System;

namespace ECM.Document.Domain.Tags;

public sealed class TagNamespace
{
    private readonly List<TagLabel> _labels = [];

    private TagNamespace()
    {
        NamespaceSlug = null!;
        Kind = null!;
    }

    private TagNamespace(
        Guid id,
        string namespaceSlug,
        string kind,
        Guid? ownerUserId,
        string? displayName,
        string? description,
        DateTimeOffset createdAtUtc)
        : this()
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Namespace identifier is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(namespaceSlug))
        {
            throw new ArgumentException("Namespace slug is required.", nameof(namespaceSlug));
        }

        if (string.IsNullOrWhiteSpace(kind))
        {
            throw new ArgumentException("Namespace kind is required.", nameof(kind));
        }

        if (createdAtUtc == default)
        {
            throw new ArgumentException("Creation timestamp is required.", nameof(createdAtUtc));
        }

        var normalizedKind = kind.Trim();
        if (!string.Equals(normalizedKind, "system", StringComparison.Ordinal)
            && !string.Equals(normalizedKind, "user", StringComparison.Ordinal))
        {
            throw new ArgumentException("Namespace kind must be either 'system' or 'user'.", nameof(kind));
        }

        Id = id;
        NamespaceSlug = namespaceSlug.Trim();
        Kind = normalizedKind;
        OwnerUserId = ownerUserId;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        CreatedAtUtc = createdAtUtc;
    }

    public static TagNamespace Create(
        string namespaceSlug,
        string kind,
        Guid? ownerUserId,
        string? displayName,
        string? description,
        DateTimeOffset createdAtUtc)
    {
        return new TagNamespace(Guid.NewGuid(), namespaceSlug, kind, ownerUserId, displayName, description, createdAtUtc);
    }

    public Guid Id { get; private set; }

    public string NamespaceSlug { get; private set; }

    public string Kind { get; private set; }

    public Guid? OwnerUserId { get; private set; }

    public string? DisplayName { get; private set; }

    public string? Description { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<TagLabel> Labels => _labels.AsReadOnly();
}
