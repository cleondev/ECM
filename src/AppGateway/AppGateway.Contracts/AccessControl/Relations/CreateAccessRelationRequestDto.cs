namespace AppGateway.Contracts.AccessControl.Relations;

using System;

public sealed class CreateAccessRelationRequestDto
{
    public Guid SubjectId { get; init; }

    public string ObjectType { get; init; } = string.Empty;

    public Guid ObjectId { get; init; }

    public string Relation { get; init; } = string.Empty;
}
