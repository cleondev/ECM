namespace ECM.AccessControl.Application.Users.Commands;

using System;

public sealed record RemoveUserRoleCommand(Guid UserId, Guid RoleId);
