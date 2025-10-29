namespace ECM.IAM.Application.Relations.Commands;

using System;

public sealed record CreateAccessRelationCommand(
    string SubjectType,
    Guid SubjectId,
    string ObjectType,
    Guid ObjectId,
    string Relation,
    DateTimeOffset? ValidFromUtc,
    DateTimeOffset? ValidToUtc);
