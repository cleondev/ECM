namespace ECM.AccessControl.Application.Users.Commands;

using System;
using System.Collections.Generic;

public sealed record CreateUserCommand(
    string Email,
    string DisplayName,
    string? Department,
    bool IsActive,
    IReadOnlyCollection<Guid> RoleIds);
