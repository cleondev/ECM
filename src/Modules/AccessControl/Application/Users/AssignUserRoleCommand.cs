namespace ECM.Modules.AccessControl.Application.Users;

using System;

public sealed record AssignUserRoleCommand(Guid UserId, Guid RoleId);
