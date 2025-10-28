namespace ECM.IAM.Application.Relations;

using System;

public sealed record AccessRelationSummaryResult(
    string SubjectType,
    Guid SubjectId,
    string ObjectType,
    Guid ObjectId,
    string Relation,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ValidFromUtc,
    DateTimeOffset? ValidToUtc);
