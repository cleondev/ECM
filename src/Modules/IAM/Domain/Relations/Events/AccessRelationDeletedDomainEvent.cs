using ECM.BuildingBlocks.Domain.Events;

namespace ECM.IAM.Domain.Relations.Events;

public sealed record AccessRelationDeletedDomainEvent(
    Guid SubjectId,
    string ObjectType,
    Guid ObjectId,
    string Relation,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
