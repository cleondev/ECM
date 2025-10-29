namespace ECM.IAM.Application.Groups;

using System;
using ECM.IAM.Domain.Groups;

public sealed record GroupAssignment(string Name, GroupKind Kind, Guid? ParentGroupId = null, string Role = "member")
{
    public GroupAssignment Normalize()
    {
        var normalizedName = string.IsNullOrWhiteSpace(Name) ? throw new ArgumentException("Group name is required.", nameof(Name)) : Name.Trim();
        var normalizedRole = string.IsNullOrWhiteSpace(Role) ? "member" : Role.Trim();
        var normalizedParentGroupId = ParentGroupId == Guid.Empty ? null : ParentGroupId;

        return this with { Name = normalizedName, Role = normalizedRole, ParentGroupId = normalizedParentGroupId };
    }

    public static GroupAssignment System() => new(GroupDefaults.SystemName, GroupKind.System);

    public static GroupAssignment Guest() => new(GroupDefaults.GuestName, GroupKind.System);

    public static GroupAssignment Unit(string name, Guid? parentGroupId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Unit name is required.", nameof(name));
        }

        return new GroupAssignment(name.Trim(), GroupKind.Unit, parentGroupId);
    }

    public static GroupAssignment FromString(string name, string? kind, string role = "member", Guid? parentGroupId = null)
        => new GroupAssignment(name, GroupKindExtensions.FromString(kind), parentGroupId, role).Normalize();
}
