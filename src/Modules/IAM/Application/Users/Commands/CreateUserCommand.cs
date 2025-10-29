namespace ECM.IAM.Application.Users.Commands;

using System;
using System.Collections.Generic;
using ECM.IAM.Application.Groups;

public sealed record CreateUserCommand(
    string Email,
    string DisplayName,
    IReadOnlyCollection<GroupAssignment> Groups,
    bool IsActive,
    string? Password,
    IReadOnlyCollection<Guid> RoleIds);
