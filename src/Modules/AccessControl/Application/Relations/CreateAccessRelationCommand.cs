namespace ECM.Modules.AccessControl.Application.Relations;

using System;

public sealed record CreateAccessRelationCommand(
    Guid SubjectId,
    string ObjectType,
    Guid ObjectId,
    string Relation);
