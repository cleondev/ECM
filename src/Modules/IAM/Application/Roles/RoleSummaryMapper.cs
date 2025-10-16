using System;
using ECM.IAM.Domain.Roles;
using ECM.IAM.Domain.Users;

namespace ECM.IAM.Application.Roles;

internal static class RoleSummaryMapper
{
    public static RoleSummary FromRole(Role role)
    {
        ArgumentNullException.ThrowIfNull(role);

        return new RoleSummary(role.Id, role.Name);
    }

    public static RoleSummary FromLink(UserRole link)
    {
        ArgumentNullException.ThrowIfNull(link);

        var name = link.Role?.Name ?? string.Empty;
        return new RoleSummary(link.RoleId, name);
    }
}
