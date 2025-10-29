namespace AppGateway.Contracts.IAM.Groups;

using System;

public sealed class GroupAssignmentDto
{
    public Guid? GroupId { get; init; }

    public string? Identifier { get; init; }

    public string? Kind { get; init; }

    public Guid? ParentGroupId { get; init; }

    public string? Role { get; init; }
}
