using System;
using System.Collections.Generic;

namespace ECM.IAM.Domain.Groups;

public sealed class Group
{
    private Group()
    {
        Name = null!;
        Kind = GroupKinds.Normal;
        Members = new List<GroupMember>();
    }

    private Group(Guid id, string name, string kind, Guid? createdBy, DateTimeOffset createdAtUtc)
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

    public string Kind { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public ICollection<GroupMember> Members { get; }

    public static Group CreateSystemGroup(string name, DateTimeOffset createdAtUtc, Guid? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Group name is required.", nameof(name));
        }

        var normalizedName = NormalizeName(name);
        var groupId = GroupDefaults.TryGetIdForName(normalizedName, out var id)
            ? id
            : Guid.NewGuid();

        return new Group(groupId, normalizedName, GroupKinds.System, createdBy, createdAtUtc);
    }

    public static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        return name.Trim().ToLowerInvariant();
    }
}
