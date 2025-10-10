namespace AppGateway.Contracts.AccessControl.Relations;

using System;

public sealed record AccessRelationDto(
    Guid SubjectId,
    string ObjectType,
    Guid ObjectId,
    string Relation,
    DateTimeOffset CreatedAtUtc);
