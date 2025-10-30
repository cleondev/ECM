using System;
using System.Collections.Generic;
using System.Linq;
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
        PathIds = [];
        Name = null!;
    }

    private TagLabel(
        Guid id,
        Guid namespaceId,
        Guid? parentId,
        Guid[] pathIds,
        string name,
        int sortOrder,
        string? color,
        string? iconKey,
        bool isActive,
        bool isSystem,
        Guid? createdBy,
        DateTimeOffset createdAtUtc)
        : this()
    {
        if (namespaceId == Guid.Empty)
        {
            throw new ArgumentException("Namespace identifier is required.", nameof(namespaceId));
        }

        if (pathIds is null || pathIds.Length == 0)
        {
            throw new ArgumentException("Path identifiers must contain at least the tag identifier.", nameof(pathIds));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Tag name is required.", nameof(name));
        }

        if (createdAtUtc == default)
        {
            throw new ArgumentException("Creation timestamp is required.", nameof(createdAtUtc));
        }

        if (parentId == Guid.Empty)
        {
            parentId = null;
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        NamespaceId = namespaceId;
        ParentId = parentId;
        PathIds = pathIds;
        Name = name.Trim();
        SortOrder = sortOrder;
        Color = NormalizeOptional(color);
        IconKey = NormalizeOptional(iconKey);
        IsActive = isActive;
        IsSystem = isSystem;
        CreatedBy = createdBy;
        CreatedAtUtc = createdAtUtc;
    }

    public static TagLabel Create(
        Guid namespaceId,
        Guid? parentId,
        Guid[]? parentPathIds,
        string name,
        int sortOrder,
        string? color,
        string? iconKey,
        Guid? createdBy,
        bool isSystem,
        DateTimeOffset createdAtUtc)
    {
        var tagId = Guid.NewGuid();
        var parentPath = parentPathIds is null ? [] : parentPathIds.Where(id => id != Guid.Empty).ToArray();
        var path = BuildPath(parentPath, tagId);

        var tagLabel = new TagLabel(
            tagId,
            namespaceId,
            NormalizeParent(parentId),
            path,
            name,
            sortOrder,
            color,
            iconKey,
            isActive: true,
            isSystem,
            createdBy,
            createdAtUtc);

        tagLabel.Raise(new TagLabelCreatedDomainEvent(
            tagLabel.Id,
            tagLabel.NamespaceId,
            tagLabel.ParentId,
            tagLabel.Name,
            tagLabel.PathIds,
            tagLabel.SortOrder,
            tagLabel.Color,
            tagLabel.IconKey,
            tagLabel.IsSystem,
            tagLabel.CreatedBy,
            tagLabel.CreatedAtUtc));

        return tagLabel;
    }

    public Guid Id { get; private set; }

    public Guid NamespaceId { get; private set; }

    public TagNamespace? Namespace { get; private set; }

    public Guid? ParentId { get; private set; }

    public TagLabel? Parent { get; private set; }

    public Guid[] PathIds { get; private set; }

    public string Name { get; private set; }

    public int SortOrder { get; private set; }

    public string? Color { get; private set; }

    public string? IconKey { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsSystem { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<DocumentTag> DocumentTags => _documentTags.AsReadOnly();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void Update(
        string name,
        Guid? parentId,
        Guid[]? parentPathIds, // Changed from IReadOnlyCollection<Guid>? to Guid[]?
        int sortOrder,
        string? color,
        string? iconKey,
        bool isActive,
        Guid? updatedBy,
        DateTimeOffset updatedAtUtc)
    {
        if (IsSystem)
        {
            throw new InvalidOperationException("System tags cannot be modified.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Tag name is required.", nameof(name));
        }

        if (updatedAtUtc == default)
        {
            throw new ArgumentException("Update timestamp is required.", nameof(updatedAtUtc));
        }

        var normalizedParentId = NormalizeParent(parentId);
        var normalizedName = name.Trim();
        var normalizedColor = NormalizeOptional(color);
        var normalizedIconKey = NormalizeOptional(iconKey);

        if (parentPathIds is not null)
        {
            var parentPath = parentPathIds.Where(id => id != Guid.Empty).ToArray();
            PathIds = BuildPath(parentPath, Id);
        }
        else if (PathIds.Length == 0 || PathIds[^1] != Id)
        {
            PathIds = BuildPath([], Id);
        }

        ParentId = normalizedParentId;
        Name = normalizedName;
        SortOrder = sortOrder;
        Color = normalizedColor;
        IconKey = normalizedIconKey;
        IsActive = isActive;

        Raise(new TagLabelUpdatedDomainEvent(
            Id,
            NamespaceId,
            ParentId,
            Name,
            PathIds,
            SortOrder,
            Color,
            IconKey,
            IsActive,
            updatedBy,
            updatedAtUtc));
    }

    public void MarkDeleted(DateTimeOffset deletedAtUtc)
    {
        if (IsSystem)
        {
            throw new InvalidOperationException("System tags cannot be deleted.");
        }

        Raise(new TagLabelDeletedDomainEvent(Id, NamespaceId, deletedAtUtc));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    private void Raise(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    private static Guid[] BuildPath(Guid[] parentPathIds, Guid tagId)
    {
        var buffer = new List<Guid>(parentPathIds.Length + 1);
        foreach (var ancestorId in parentPathIds)
        {
            if (ancestorId == Guid.Empty)
            {
                continue;
            }

            if (buffer.Contains(ancestorId))
            {
                continue;
            }

            buffer.Add(ancestorId);
        }

        buffer.Add(tagId);
        return [.. buffer];
    }

    private static Guid? NormalizeParent(Guid? parentId)
    {
        if (parentId is null)
        {
            return null;
        }

        return parentId.Value == Guid.Empty ? null : parentId;
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
