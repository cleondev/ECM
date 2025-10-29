namespace ECM.IAM.Application.Groups;

using System;
using ECM.IAM.Domain.Groups;

public sealed record GroupAssignment(
    Guid? GroupId,
    string? Identifier,
    GroupKind Kind,
    Guid? ParentGroupId = null,
    string Role = "member")
{
    public GroupAssignment Normalize()
    {
        var normalizedRole = string.IsNullOrWhiteSpace(Role) ? "member" : Role.Trim();
        var normalizedParentGroupId = ParentGroupId == Guid.Empty ? null : ParentGroupId;
        var normalizedIdentifier = string.IsNullOrWhiteSpace(Identifier) ? null : Identifier.Trim();
        var normalizedGroupId = GroupId.HasValue && GroupId.Value == Guid.Empty ? null : GroupId;

        return this with
        {
            Identifier = normalizedIdentifier,
            Role = normalizedRole,
            ParentGroupId = normalizedParentGroupId,
            GroupId = normalizedGroupId
        };
    }

    public static GroupAssignment System()
        => new(GroupDefaults.SystemId, GroupDefaults.SystemName, GroupKind.System);

    public static GroupAssignment Guest()
        => new(GroupDefaults.GuestId, GroupDefaults.GuestName, GroupKind.System);

    public static GroupAssignment Unit(string identifier, Guid? parentGroupId = null)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("Unit identifier is required.", nameof(identifier));
        }

        return new GroupAssignment(null, identifier.Trim(), GroupKind.Unit, parentGroupId);
    }

    public static GroupAssignment FromContract(
        Guid? groupId,
        string? identifier,
        string? kind,
        string role = "member",
        Guid? parentGroupId = null)
        => new GroupAssignment(groupId, identifier, GroupKindExtensions.FromString(kind), parentGroupId, role).Normalize();
}
