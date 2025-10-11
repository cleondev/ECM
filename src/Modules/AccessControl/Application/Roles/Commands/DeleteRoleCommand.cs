using System;

namespace ECM.AccessControl.Application.Roles.Commands;

public sealed record DeleteRoleCommand(Guid RoleId);
