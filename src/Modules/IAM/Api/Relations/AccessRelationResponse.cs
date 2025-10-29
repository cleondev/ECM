namespace ECM.IAM.Api.Relations;

using System;

public sealed record AccessRelationResponse(
    string SubjectType,
    Guid SubjectId,
    string ObjectType,
    Guid ObjectId,
    string Relation,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ValidFromUtc,
    DateTimeOffset? ValidToUtc);
