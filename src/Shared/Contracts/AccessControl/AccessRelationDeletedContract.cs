namespace Shared.Contracts.AccessControl;

public sealed record AccessRelationDeletedContract(
    Guid SubjectId,
    string ObjectType,
    Guid ObjectId,
    string Relation,
    DateTimeOffset DeletedAtUtc);
