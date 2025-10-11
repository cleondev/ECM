using ECM.BuildingBlocks.Domain.Events;

namespace ECM.Document.Domain.Tags.Events;

public sealed record TagLabelDeletedDomainEvent(
    Guid TagId,
    string NamespaceSlug,
    string Path,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
