namespace AppGateway.Contracts.IAM.Users;

using System;
using System.Collections.Generic;
using AppGateway.Contracts.IAM.Groups;

public sealed class UpdateUserProfileRequestDto
{
    public string DisplayName { get; set; } = string.Empty;

    public IReadOnlyCollection<GroupAssignmentDto> Groups { get; set; }
        = Array.Empty<GroupAssignmentDto>();
}
