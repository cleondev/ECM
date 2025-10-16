using System;
using System.Linq;
using ECM.IAM.Application.Roles;
using ECM.IAM.Domain.Users;

namespace ECM.IAM.Application.Users;

internal static class Mapping
{
    public static UserSummaryResult ToResult(this User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var roles = user.Roles
            .Select(link => link.ToResult())
            .ToArray();

        return new UserSummaryResult(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Department,
            user.IsActive,
            user.CreatedAtUtc,
            roles);
    }
}
