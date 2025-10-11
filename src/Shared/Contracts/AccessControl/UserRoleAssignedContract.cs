namespace Shared.Contracts.AccessControl;

public sealed record UserRoleAssignedContract(
    Guid UserId,
    Guid RoleId,
    string RoleName,
    DateTimeOffset AssignedAtUtc);
