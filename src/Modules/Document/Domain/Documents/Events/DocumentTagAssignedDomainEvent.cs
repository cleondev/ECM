using ECM.BuildingBlocks.Domain.Events;

namespace ECM.Document.Domain.Documents.Events;

public sealed record DocumentTagAssignedDomainEvent(
    DocumentId DocumentId,
    Guid TagId,
    Guid? AppliedBy,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
