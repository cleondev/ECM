using System.Linq;
using ECM.AccessControl.Application.Users;

namespace ECM.AccessControl.Api.Users;

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
