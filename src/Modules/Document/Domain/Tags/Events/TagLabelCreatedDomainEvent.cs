using System;
using System.Collections.Generic;
using ECM.BuildingBlocks.Domain.Events;

namespace ECM.Document.Domain.Tags.Events;

public sealed record TagLabelCreatedDomainEvent(
    Guid TagId,
    Guid NamespaceId,
    Guid? ParentId,
    string Name,
    IReadOnlyList<Guid> PathIds,
    int SortOrder,
    string? Color,
    string? IconKey,
    bool IsSystem,
    Guid? CreatedBy,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
