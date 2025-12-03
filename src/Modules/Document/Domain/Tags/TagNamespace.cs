using System;
using System.Collections.Generic;

namespace ECM.Document.Domain.Tags;

public sealed class TagNamespace
{
    private readonly List<TagLabel> _labels = [];

    private TagNamespace()
    {
        Scope = null!;
    }

    private TagNamespace(
        Guid id,
        string scope,
        Guid? ownerUserId,
        Guid? ownerGroupId,
        string? displayName,
        bool isSystem,
        DateTimeOffset createdAtUtc)
        : this()
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Namespace identifier is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(scope))
        {
            throw new ArgumentException("Namespace scope is required.", nameof(scope));
        }

        if (createdAtUtc == default)
        {
            throw new ArgumentException("Creation timestamp is required.", nameof(createdAtUtc));
        }

        var normalizedScope = scope.Trim().ToLowerInvariant();
        if (!string.Equals(normalizedScope, "global", StringComparison.Ordinal)
            && !string.Equals(normalizedScope, "group", StringComparison.Ordinal)
            && !string.Equals(normalizedScope, "user", StringComparison.Ordinal))
        {
            throw new ArgumentException("Namespace scope must be one of: global, group, user.", nameof(scope));
        }

        var normalizedOwnerUserId = NormalizeOwner(ownerUserId);
        var normalizedOwnerGroupId = NormalizeOwner(ownerGroupId);

        if (string.Equals(normalizedScope, "user", StringComparison.Ordinal) && normalizedOwnerUserId is null)
        {
            throw new ArgumentException("User namespaces must specify the owning user identifier.", nameof(ownerUserId));
        }

        if (string.Equals(normalizedScope, "group", StringComparison.Ordinal) && normalizedOwnerGroupId is null)
        {
            throw new ArgumentException("Group namespaces must specify the owning group identifier.", nameof(ownerGroupId));
        }

        Id = id;
        Scope = normalizedScope;
        OwnerUserId = normalizedOwnerUserId;
        OwnerGroupId = normalizedOwnerGroupId;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        IsSystem = isSystem;
        CreatedAtUtc = createdAtUtc;
    }

    public static TagNamespace Create(
        string scope,
        Guid? ownerUserId,
        Guid? ownerGroupId,
        string? displayName,
        bool isSystem,
        DateTimeOffset createdAtUtc)
    {
        return new TagNamespace(Guid.NewGuid(), scope, ownerUserId, ownerGroupId, displayName, isSystem, createdAtUtc);
    }

    public Guid Id { get; private set; }

    public string Scope { get; private set; }

    public Guid? OwnerUserId { get; private set; }

    public Guid? OwnerGroupId { get; private set; }

    public string? DisplayName { get; private set; }

    public bool IsSystem { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<TagLabel> Labels => _labels.AsReadOnly();

    public void Rename(string? displayName)
    {
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
    }

    private static Guid? NormalizeOwner(Guid? identifier)
    {
        if (identifier is null)
        {
            return null;
        }

        return identifier.Value == Guid.Empty ? null : identifier;
    }
}
