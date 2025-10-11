namespace ECM.AccessControl.Application.Relations.Commands;

using System;

public sealed record DeleteAccessRelationCommand(
    Guid SubjectId,
    string ObjectType,
    Guid ObjectId,
    string Relation);
