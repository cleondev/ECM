using System;
using System.Linq;
using ECM.AccessControl.Application.Roles;
using ECM.AccessControl.Domain.Users;

namespace ECM.AccessControl.Application.Users;

internal static class UserSummaryMapper
{
    public static UserSummary ToSummary(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var roles = user.Roles
            .Select(RoleSummaryMapper.FromLink)
            .ToArray();

        return new UserSummary(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Department,
            user.IsActive,
            user.CreatedAtUtc,
            roles);
    }
}
