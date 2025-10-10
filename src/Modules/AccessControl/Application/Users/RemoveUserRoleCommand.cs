namespace ECM.Modules.AccessControl.Application.Users;

using System;

public sealed record RemoveUserRoleCommand(Guid UserId, Guid RoleId);
