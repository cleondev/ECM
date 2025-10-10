namespace ECM.Modules.AccessControl.Application.Users;

using System;
using System.Collections.Generic;

public sealed record CreateUserCommand(
    string Email,
    string DisplayName,
    string? Department,
    bool IsActive,
    IReadOnlyCollection<Guid> RoleIds);
