namespace AppGateway.Contracts.IAM.Users;

using System;
using System.Collections.Generic;
using AppGateway.Contracts.IAM.Groups;

public sealed class CreateUserRequestDto
{
    public string Email { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public IReadOnlyCollection<GroupAssignmentDto> Groups { get; init; } = Array.Empty<GroupAssignmentDto>();

    public bool IsActive { get; init; } = true;

    public string? Password { get; init; }

    public IReadOnlyCollection<Guid> RoleIds { get; init; } = [];
}
