namespace Shared.Contracts.IAM;

public sealed record AccessRelationDeletedContract(
    string SubjectType,
    Guid SubjectId,
    string ObjectType,
    Guid ObjectId,
    string Relation,
    DateTimeOffset? ValidToUtc,
    DateTimeOffset DeletedAtUtc);
