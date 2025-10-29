namespace ECM.IAM.Api.Groups;

using System;
using ECM.IAM.Application.Groups;

public sealed class GroupAssignmentRequest
{
    public Guid? GroupId { get; init; }

    public string? Identifier { get; init; }

    public string? Kind { get; init; }

    public Guid? ParentGroupId { get; init; }

    public string? Role { get; init; }

    public GroupAssignment ToAssignment()
        => GroupAssignment.FromContract(GroupId, Identifier, Kind, Role ?? "member", ParentGroupId);
}
