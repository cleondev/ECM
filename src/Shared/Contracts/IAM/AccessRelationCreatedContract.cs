namespace Shared.Contracts.IAM;

public sealed record AccessRelationCreatedContract(
    Guid SubjectId,
    string ObjectType,
    Guid ObjectId,
    string Relation,
    DateTimeOffset CreatedAtUtc);
