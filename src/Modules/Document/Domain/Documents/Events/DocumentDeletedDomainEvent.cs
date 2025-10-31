using System;
using ECM.BuildingBlocks.Domain.Events;
using ECM.Document.Domain.Documents;

namespace ECM.Document.Domain.Documents.Events;

public sealed record DocumentDeletedDomainEvent(
    DocumentId DocumentId,
    Guid DeletedBy,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
