using System;

namespace ECM.IAM.Domain.Groups;

public sealed class GroupMember
{
    private GroupMember()
    {
        Role = GroupMemberRoles.Member;
    }

    private GroupMember(Guid groupId, Guid userId, string role, DateTimeOffset validFromUtc, DateTimeOffset? validToUtc)
        : this()
    {
        GroupId = groupId;
        UserId = userId;
        Role = role;
        ValidFromUtc = validFromUtc;
        ValidToUtc = validToUtc;
    }

    public Guid GroupId { get; private set; }

    public Guid UserId { get; private set; }

    public string Role { get; private set; }

    public DateTimeOffset ValidFromUtc { get; private set; }

    public DateTimeOffset? ValidToUtc { get; private set; }

    public Group Group { get; private set; } = null!;

    public static GroupMember Create(
        Guid groupId,
        Guid userId,
        DateTimeOffset validFromUtc,
        string role = GroupMemberRoles.Member,
        DateTimeOffset? validToUtc = null)
    {
        if (groupId == Guid.Empty)
        {
            throw new ArgumentException("Group id is required.", nameof(groupId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        var normalizedRole = string.IsNullOrWhiteSpace(role)
            ? GroupMemberRoles.Member
            : role.Trim().ToLowerInvariant();

        return new GroupMember(groupId, userId, normalizedRole, validFromUtc, validToUtc);
    }
}

public static class GroupMemberRoles
{
    public const string Member = "member";
}
