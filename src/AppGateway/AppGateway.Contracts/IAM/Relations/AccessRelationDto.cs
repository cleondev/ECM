namespace AppGateway.Contracts.IAM.Relations;

using System;

public sealed record AccessRelationDto(
    string SubjectType,
    Guid SubjectId,
    string ObjectType,
    Guid ObjectId,
    string Relation,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ValidFromUtc,
    DateTimeOffset? ValidToUtc);
