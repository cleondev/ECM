namespace Shared.Contracts.IAM;

public sealed record AccessRelationDeletedContract(
    Guid SubjectId,
    string ObjectType,
    Guid ObjectId,
    string Relation,
    DateTimeOffset DeletedAtUtc);
