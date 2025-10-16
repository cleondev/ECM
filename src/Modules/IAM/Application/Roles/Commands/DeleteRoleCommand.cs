using System;

namespace ECM.IAM.Application.Roles.Commands;

public sealed record DeleteRoleCommand(Guid RoleId);
