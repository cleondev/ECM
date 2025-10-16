namespace ECM.IAM.Application.Relations;

using System;

public sealed record AccessRelationSummaryResult(
    Guid SubjectId,
    string ObjectType,
    Guid ObjectId,
    string Relation,
    DateTimeOffset CreatedAtUtc);
