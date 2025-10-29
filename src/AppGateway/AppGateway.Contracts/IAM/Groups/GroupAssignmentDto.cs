namespace AppGateway.Contracts.IAM.Groups;

using System;

public sealed class GroupAssignmentDto
{
    public string Name { get; init; } = string.Empty;

    public string? Kind { get; init; }

    public Guid? ParentGroupId { get; init; }

    public string? Role { get; init; }
}
