using ECM.BuildingBlocks.Domain.Events;

namespace ECM.IAM.Domain.Relations.Events;

public sealed record AccessRelationDeletedDomainEvent(
    string SubjectType,
    Guid SubjectId,
    string ObjectType,
    Guid ObjectId,
    string Relation,
    DateTimeOffset? ValidToUtc,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
