namespace Shared.Contracts.IAM;

public sealed record AccessRelationCreatedContract(
    string SubjectType,
    Guid SubjectId,
    string ObjectType,
    Guid ObjectId,
    string Relation,
    DateTimeOffset ValidFromUtc,
    DateTimeOffset? ValidToUtc,
    DateTimeOffset CreatedAtUtc);
