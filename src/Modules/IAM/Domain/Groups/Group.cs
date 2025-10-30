using System;
using System.Collections.Generic;

namespace ECM.IAM.Domain.Groups;

public sealed class Group
{
    private Group()
    {
        Name = null!;
        Kind = GroupKind.Temporary;
        Members = [];
    }

    private Group(Guid id, string name, GroupKind kind, Guid? parentGroupId, Guid? createdBy, DateTimeOffset createdAtUtc)
        : this()
    {
        Id = id;
        Name = name;
        Kind = kind;
        CreatedBy = createdBy;
        CreatedAtUtc = createdAtUtc;
        SetParent(parentGroupId);
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public GroupKind Kind { get; private set; }

    public Guid? ParentGroupId { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public ICollection<GroupMember> Members { get; }

    public static Group Create(string name, GroupKind kind, Guid? createdBy, DateTimeOffset createdAtUtc, Guid? parentGroupId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Group name is required.", nameof(name));
        }

        var normalizedName = name.Trim();
        return new Group(Guid.NewGuid(), normalizedName, kind, parentGroupId, createdBy, createdAtUtc);
    }

    public static Group Create(string name, string? kind, Guid? createdBy, DateTimeOffset createdAtUtc, Guid? parentGroupId = null)
        => Create(name, GroupKindExtensions.FromString(kind), createdBy, createdAtUtc, parentGroupId);

    public static Group CreateSystemGroup(string name, DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Group name is required.", nameof(name));
        }

        var normalizedName = name.Trim();
        var id = GroupDefaults.TryGetIdForName(normalizedName, out var knownId)
            ? knownId
            : Guid.NewGuid();

        return new Group(id, normalizedName, GroupKind.System, parentGroupId: null, createdBy: null, createdAtUtc);
    }

    public static Group CreateGuessGroup(Guid parentGroupId, DateTimeOffset createdAtUtc)
    {
        if (parentGroupId == Guid.Empty)
        {
            throw new ArgumentException("Parent group id is required.", nameof(parentGroupId));
        }

        return new Group(
            GroupDefaults.GuessUserId,
            GroupDefaults.GuessUserName,
            GroupKind.Guess,
            parentGroupId,
            createdBy: null,
            createdAtUtc);
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Group name is required.", nameof(name));
        }

        Name = name.Trim();
    }

    public void SetParent(Guid? parentGroupId)
    {
        if (parentGroupId == Guid.Empty)
        {
            parentGroupId = null;
        }

        if (parentGroupId.HasValue && parentGroupId.Value == Id)
        {
            throw new InvalidOperationException("A group cannot be its own parent.");
        }

        ParentGroupId = parentGroupId;
    }
}
