namespace ECM.Modules.AccessControl.Application.Relations;

using System;

public sealed record DeleteAccessRelationCommand(
    Guid SubjectId,
    string ObjectType,
    Guid ObjectId,
    string Relation);
