namespace ECM.IAM.Api.Groups;

using System;
using ECM.IAM.Application.Groups;

public sealed class GroupAssignmentRequest
{
    public string Name { get; init; } = string.Empty;

    public string? Kind { get; init; }

    public Guid? ParentGroupId { get; init; }

    public string? Role { get; init; }

    public GroupAssignment ToAssignment()
        => GroupAssignment.FromString(Name, Kind, Role ?? "member", ParentGroupId);
}
