namespace AppGateway.Contracts.IAM.Users;

using System;
using System.Collections.Generic;
using AppGateway.Contracts.IAM.Groups;

public sealed class UpdateUserRequestDto
{
    public string DisplayName { get; init; } = string.Empty;

    public IReadOnlyCollection<GroupAssignmentDto> Groups { get; init; } = Array.Empty<GroupAssignmentDto>();

    public bool? IsActive { get; init; }
}
