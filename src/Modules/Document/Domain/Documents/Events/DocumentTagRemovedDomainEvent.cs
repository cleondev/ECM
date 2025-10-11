using ECM.BuildingBlocks.Domain.Events;

namespace ECM.Document.Domain.Documents.Events;

public sealed record DocumentTagRemovedDomainEvent(
    DocumentId DocumentId,
    Guid TagId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
