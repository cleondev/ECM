namespace ECM.IAM.Application.Relations;

using System;

public sealed record AccessRelationSummary(
    Guid SubjectId,
    string ObjectType,
    Guid ObjectId,
    string Relation,
    DateTimeOffset CreatedAtUtc);
