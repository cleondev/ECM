using System;
using ECM.IAM.Domain.Roles;
using ECM.IAM.Domain.Users;

namespace ECM.IAM.Application.Roles;

internal static class Mapping
{
    public static RoleSummaryResult ToResult(this Role role)
    {
        ArgumentNullException.ThrowIfNull(role);

        return new RoleSummaryResult(role.Id, role.Name);
    }

    public static RoleSummaryResult ToResult(this UserRole link)
    {
        ArgumentNullException.ThrowIfNull(link);

        var name = link.Role?.Name ?? string.Empty;
        return new RoleSummaryResult(link.RoleId, name);
    }
}
