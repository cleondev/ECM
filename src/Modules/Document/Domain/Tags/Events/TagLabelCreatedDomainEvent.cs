using ECM.BuildingBlocks.Domain.Events;

namespace ECM.Document.Domain.Tags.Events;

public sealed record TagLabelCreatedDomainEvent(
    Guid TagId,
    string NamespaceSlug,
    string Path,
    Guid? CreatedBy,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
