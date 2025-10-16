using ECM.BuildingBlocks.Domain.Events;

namespace ECM.IAM.Domain.Users.Events;

public sealed record UserCreatedDomainEvent(
    Guid UserId,
    string Email,
    string DisplayName,
    string? Department,
    bool IsActive,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
