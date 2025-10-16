namespace ECM.IAM.Application.Roles.Commands;

using System;

public sealed record RenameRoleCommand(Guid RoleId, string Name);
