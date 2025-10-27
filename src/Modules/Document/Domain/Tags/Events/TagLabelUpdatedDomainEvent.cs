using ECM.BuildingBlocks.Domain.Events;

namespace ECM.Document.Domain.Tags.Events;

public sealed record TagLabelUpdatedDomainEvent(
    Guid TagId,
    string NamespaceSlug,
    string Path,
    Guid? UpdatedBy,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
