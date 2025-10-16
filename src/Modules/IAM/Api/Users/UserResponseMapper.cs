using System.Linq;

using ECM.IAM.Api.Roles;
using ECM.IAM.Application.Users;

namespace ECM.IAM.Api.Users;

internal static class UserResponseMapper
{
    public static UserResponse Map(UserSummary summary)
        => new(
            summary.Id,
            summary.Email,
            summary.DisplayName,
            summary.Department,
            summary.IsActive,
            summary.CreatedAtUtc,
            [.. summary.Roles.Select(role => new RoleResponse(role.Id, role.Name))]);
}
