namespace Shared.Contracts.IAM;

public sealed record UserRoleRemovedContract(
    Guid UserId,
    Guid RoleId,
    DateTimeOffset RemovedAtUtc);
