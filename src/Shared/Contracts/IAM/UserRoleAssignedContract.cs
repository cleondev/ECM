namespace Shared.Contracts.IAM;

public sealed record UserRoleAssignedContract(
    Guid UserId,
    Guid RoleId,
    string RoleName,
    DateTimeOffset AssignedAtUtc);
