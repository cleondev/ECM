namespace ECM.IAM.Application.Users.Commands;

using System;

public sealed record AssignUserRoleCommand(Guid UserId, Guid RoleId);
