namespace ECM.Modules.AccessControl.Api.Relations;

using System;

public sealed record AccessRelationResponse(
    Guid SubjectId,
    string ObjectType,
    Guid ObjectId,
    string Relation,
    DateTimeOffset CreatedAtUtc);
