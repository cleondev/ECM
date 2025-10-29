namespace ECM.IAM.Api.Users;

using System;
using System.Collections.Generic;
using ECM.IAM.Api.Groups;

public sealed class CreateUserRequest
{
    public string Email { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public IReadOnlyCollection<GroupAssignmentRequest> Groups { get; init; } = Array.Empty<GroupAssignmentRequest>();

    public bool IsActive { get; init; } = true;

    public string? Password { get; init; }

    public IReadOnlyCollection<Guid> RoleIds { get; init; } = [];
}
