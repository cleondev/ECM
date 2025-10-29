using System;
using System.Collections.Generic;

namespace ECM.IAM.Domain.Groups;

public sealed class Group
{
    private Group()
    {
        Name = null!;
        Kind = GroupKind.Temporary;
        Members = new List<GroupMember>();
    }

    private Group(Guid id, string name, GroupKind kind, Guid? createdBy, DateTimeOffset createdAtUtc)
        : this()
    {
        Id = id;
        Name = name;
        Kind = kind;
        CreatedBy = createdBy;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public GroupKind Kind { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public ICollection<GroupMember> Members { get; }

    public static Group Create(string name, GroupKind kind, Guid? createdBy, DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Group name is required.", nameof(name));
        }

        var normalizedName = name.Trim();
        return new Group(Guid.NewGuid(), normalizedName, kind, createdBy, createdAtUtc);
    }

    public static Group Create(string name, string? kind, Guid? createdBy, DateTimeOffset createdAtUtc)
        => Create(name, GroupKindExtensions.FromString(kind), createdBy, createdAtUtc);

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

        return new Group(id, normalizedName, GroupKind.System, createdBy: null, createdAtUtc);
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Group name is required.", nameof(name));
        }

        Name = name.Trim();
    }
}
