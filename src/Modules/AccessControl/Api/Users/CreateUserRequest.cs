namespace ECM.Modules.AccessControl.Api.Users;

using System;
using System.Collections.Generic;

public sealed class CreateUserRequest
{
    public string Email { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string? Department { get; init; }

    public bool IsActive { get; init; } = true;

    public IReadOnlyCollection<Guid> RoleIds { get; init; } = Array.Empty<Guid>();
}
