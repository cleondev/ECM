using ECM.BuildingBlocks.Domain.Events;

namespace ECM.AccessControl.Domain.Relations.Events;

public sealed record AccessRelationCreatedDomainEvent(
    Guid SubjectId,
    string ObjectType,
    Guid ObjectId,
    string Relation,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
