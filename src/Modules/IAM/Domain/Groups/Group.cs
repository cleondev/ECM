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

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string Kind { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public ICollection<GroupMember> Members { get; }
}
