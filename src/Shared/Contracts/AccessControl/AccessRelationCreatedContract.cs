namespace Shared.Contracts.AccessControl;

public sealed record AccessRelationCreatedContract(
    Guid SubjectId,
    string ObjectType,
    Guid ObjectId,
    string Relation,
    DateTimeOffset CreatedAtUtc);
