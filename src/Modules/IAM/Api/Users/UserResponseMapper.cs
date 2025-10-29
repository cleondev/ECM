using System.Linq;

using ECM.IAM.Api.Groups;
using ECM.IAM.Api.Roles;
using ECM.IAM.Application.Users;

namespace ECM.IAM.Api.Users;

internal static class UserResponseMapper
{
    public static UserResponse Map(UserSummaryResult summary)
    {
        var roles = summary.Roles
            .Select(role => new RoleResponse(role.Id, role.Name))
            .ToArray();

        var groups = summary.Groups
            .Select(group => new GroupResponse(group.Id, group.Name, group.Kind, group.Role))
            .ToArray();

        return new UserResponse(
            summary.Id,
            summary.Email,
            summary.DisplayName,
            summary.Department,
            summary.IsActive,
            summary.CreatedAtUtc,
            roles,
            groups);
    }
}
