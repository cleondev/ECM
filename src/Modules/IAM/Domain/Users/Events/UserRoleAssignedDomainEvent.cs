using ECM.BuildingBlocks.Domain.Events;

namespace ECM.IAM.Domain.Users.Events;

public sealed record UserRoleAssignedDomainEvent(
    Guid UserId,
    Guid RoleId,
    string RoleName,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
