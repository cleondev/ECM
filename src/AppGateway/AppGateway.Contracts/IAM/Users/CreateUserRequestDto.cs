namespace AppGateway.Contracts.IAM.Users;

using System;
using System.Collections.Generic;

public sealed class CreateUserRequestDto
{
    public string Email { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string? Department { get; init; }

    public bool IsActive { get; init; } = true;

    public string? Password { get; init; }

    public IReadOnlyCollection<Guid> RoleIds { get; init; } = [];
}
