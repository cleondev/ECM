namespace AppGateway.Contracts.IAM.Relations;

using System;

public sealed class CreateAccessRelationRequestDto
{
    public string SubjectType { get; init; } = "user";

    public Guid SubjectId { get; init; }

    public string ObjectType { get; init; } = string.Empty;

    public Guid ObjectId { get; init; }

    public string Relation { get; init; } = string.Empty;

    public DateTimeOffset? ValidFromUtc { get; init; }

    public DateTimeOffset? ValidToUtc { get; init; }
}
