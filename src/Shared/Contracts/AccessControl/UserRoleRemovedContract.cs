namespace Shared.Contracts.AccessControl;

public sealed record UserRoleRemovedContract(
    Guid UserId,
    Guid RoleId,
    DateTimeOffset RemovedAtUtc);
