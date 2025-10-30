namespace ECM.IAM.Application.Users.Commands;

using System;

public sealed record RemoveUserRoleCommand(Guid UserId, Guid RoleId);
