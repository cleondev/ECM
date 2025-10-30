using System;
using ECM.IAM.Domain.Users;

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

    private GroupMember(Guid groupId, Guid userId, string role, DateTimeOffset validFromUtc)
        : this()
    {
        GroupId = groupId;
        UserId = userId;
        Role = role;
        ValidFromUtc = validFromUtc;
    }

    public Guid GroupId { get; private set; }

    public Guid UserId { get; private set; }

    public string Role { get; private set; }

    public DateTimeOffset ValidFromUtc { get; private set; }

    public DateTimeOffset? ValidToUtc { get; private set; }

    public Group Group { get; private set; } = null!;

    public User User { get; private set; } = null!;

    public static GroupMember Create(Guid groupId, Guid userId, DateTimeOffset validFromUtc, string? role = null)
    {
        if (groupId == Guid.Empty)
        {
            throw new ArgumentException("Group id is required.", nameof(groupId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        var normalizedRole = string.IsNullOrWhiteSpace(role) ? "member" : role.Trim();

        return new GroupMember(groupId, userId, normalizedRole, validFromUtc);
    }

    public void Close(DateTimeOffset endedAtUtc)
    {
        if (ValidToUtc.HasValue)
        {
            return;
        }

        ValidToUtc = endedAtUtc;
    }

    public void Reopen(DateTimeOffset validFromUtc, string? role = null)
    {
        if (validFromUtc == default)
        {
            throw new ArgumentException("Valid from time is required.", nameof(validFromUtc));
        }

        ValidFromUtc = validFromUtc;
        ValidToUtc = null;

        var normalizedRole = string.IsNullOrWhiteSpace(role) ? GroupMemberRoles.Member : role.Trim();
        Role = normalizedRole;
    }
}
