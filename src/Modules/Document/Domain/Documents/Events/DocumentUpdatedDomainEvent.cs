using System;
using ECM.BuildingBlocks.Domain.Events;
using ECM.Document.Domain.Documents;

namespace ECM.Document.Domain.Documents.Events;

public sealed record DocumentUpdatedDomainEvent(
    DocumentId DocumentId,
    string Title,
    string Status,
    string Sensitivity,
    Guid? GroupId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
