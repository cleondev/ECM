using ECM.BuildingBlocks.Domain.Events;

namespace ECM.AccessControl.Domain.Users.Events;

public sealed record UserRoleRemovedDomainEvent(
    Guid UserId,
    Guid RoleId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
