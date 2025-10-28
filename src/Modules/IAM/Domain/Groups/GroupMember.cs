using System;

namespace ECM.IAM.Domain.Groups;

public sealed class GroupMember
{
    private GroupMember()
    {
        Role = "member";
    }

    public Guid GroupId { get; private set; }

    public Guid UserId { get; private set; }

    public string Role { get; private set; }

    public DateTimeOffset ValidFromUtc { get; private set; }

    public DateTimeOffset? ValidToUtc { get; private set; }

    public Group Group { get; private set; } = null!;
}
