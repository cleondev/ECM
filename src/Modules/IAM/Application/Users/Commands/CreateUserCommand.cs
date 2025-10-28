namespace ECM.IAM.Application.Users.Commands;

using System;
using System.Collections.Generic;

public sealed record CreateUserCommand(
    string Email,
    string DisplayName,
    string? Department,
    bool IsActive,
    string? Password,
    IReadOnlyCollection<Guid> RoleIds);
