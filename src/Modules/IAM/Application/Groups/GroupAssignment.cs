namespace ECM.IAM.Application.Groups;

using System;
using ECM.IAM.Domain.Groups;

public sealed record GroupAssignment(string Name, GroupKind Kind, string Role = "member")
{
    public GroupAssignment Normalize()
    {
        var normalizedName = string.IsNullOrWhiteSpace(Name) ? throw new ArgumentException("Group name is required.", nameof(Name)) : Name.Trim();
        var normalizedRole = string.IsNullOrWhiteSpace(Role) ? "member" : Role.Trim();

        return this with { Name = normalizedName, Role = normalizedRole };
    }

    public static GroupAssignment System() => new(GroupDefaults.SystemName, GroupKind.System);

    public static GroupAssignment Guest() => new(GroupDefaults.GuestName, GroupKind.System);

    public static GroupAssignment Unit(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Unit name is required.", nameof(name));
        }

        return new GroupAssignment(name.Trim(), GroupKind.Unit);
    }

    public static GroupAssignment FromString(string name, string? kind, string role = "member")
        => new GroupAssignment(name, GroupKindExtensions.FromString(kind), role).Normalize();
}
