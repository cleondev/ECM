namespace ECM.IAM.Application.Groups;

using System;

public sealed record GroupAssignment(string Name, string Kind, string Role = "member")
{
    public GroupAssignment Normalize()
    {
        var normalizedName = string.IsNullOrWhiteSpace(Name) ? throw new ArgumentException("Group name is required.", nameof(Name)) : Name.Trim();
        var normalizedKind = string.IsNullOrWhiteSpace(Kind) ? "normal" : Kind.Trim();
        var normalizedRole = string.IsNullOrWhiteSpace(Role) ? "member" : Role.Trim();

        return this with { Name = normalizedName, Kind = normalizedKind, Role = normalizedRole };
    }

    public static GroupAssignment System() => new("system", "system");

    public static GroupAssignment Guest() => new("guest", "system");

    public static GroupAssignment Unit(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Unit name is required.", nameof(name));
        }

        return new GroupAssignment(name.Trim(), "unit");
    }
}
