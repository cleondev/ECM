using System;
using ECM.BuildingBlocks.Domain.Events;

namespace ECM.Document.Domain.Tags.Events;

public sealed record TagLabelDeletedDomainEvent(
    Guid TagId,
    Guid NamespaceId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
