using System;
using System.Linq;
using ECM.IAM.Application.Roles;
using ECM.IAM.Domain.Users;

namespace ECM.IAM.Application.Users;

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
