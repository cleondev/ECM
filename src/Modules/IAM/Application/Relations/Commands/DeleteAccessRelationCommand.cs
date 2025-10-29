namespace ECM.IAM.Application.Relations.Commands;

using System;

public sealed record DeleteAccessRelationCommand(
    string SubjectType,
    Guid SubjectId,
    string ObjectType,
    Guid ObjectId,
    string Relation);
