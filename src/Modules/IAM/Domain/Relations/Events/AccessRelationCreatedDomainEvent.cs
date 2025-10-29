using ECM.BuildingBlocks.Domain.Events;

namespace ECM.IAM.Domain.Relations.Events;

public sealed record AccessRelationCreatedDomainEvent(
    string SubjectType,
    Guid SubjectId,
    string ObjectType,
    Guid ObjectId,
    string Relation,
    DateTimeOffset ValidFromUtc,
    DateTimeOffset? ValidToUtc,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
