namespace ECM.Modules.AccessControl.Api.Relations;

using System;

public sealed class CreateAccessRelationRequest
{
    public Guid SubjectId { get; init; }

    public string ObjectType { get; init; } = string.Empty;

    public Guid ObjectId { get; init; }

    public string Relation { get; init; } = string.Empty;
}
