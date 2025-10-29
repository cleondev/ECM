using System;
using System.Collections.Generic;

namespace ECM.IAM.Domain.Groups;

public sealed class Group
{
    private Group()
    {
        Name = null!;
        Kind = "normal";
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

    public static Group Create(string name, string? kind, Guid? createdBy, DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Group name is required.", nameof(name));
        }

        var normalizedName = name.Trim();
        var normalizedKind = string.IsNullOrWhiteSpace(kind)
            ? "normal"
            : kind.Trim();

        return new Group(Guid.NewGuid(), normalizedName, normalizedKind, createdBy, createdAtUtc);
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
