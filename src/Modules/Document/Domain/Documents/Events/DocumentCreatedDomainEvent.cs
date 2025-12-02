using ECM.BuildingBlocks.Domain.Events;

namespace ECM.Document.Domain.Documents.Events;

public sealed record DocumentCreatedDomainEvent(
    DocumentId DocumentId,
    string Title,
    Guid OwnerId,
    Guid CreatedBy,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
