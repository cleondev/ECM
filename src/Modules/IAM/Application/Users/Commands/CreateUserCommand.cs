namespace ECM.IAM.Application.Users.Commands;

using System;
using System.Collections.Generic;
public sealed record CreateUserCommand(
    string Email,
    string DisplayName,
    IReadOnlyCollection<Guid> GroupIds,
    Guid? PrimaryGroupId,
    bool IsActive,
    string? Password,
    IReadOnlyCollection<Guid> RoleIds);
