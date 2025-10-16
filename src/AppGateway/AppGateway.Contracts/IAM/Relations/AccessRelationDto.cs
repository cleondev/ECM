namespace AppGateway.Contracts.IAM.Relations;

using System;

public sealed record AccessRelationDto(
    Guid SubjectId,
    string ObjectType,
    Guid ObjectId,
    string Relation,
    DateTimeOffset CreatedAtUtc);
