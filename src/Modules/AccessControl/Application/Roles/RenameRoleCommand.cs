namespace ECM.Modules.AccessControl.Application.Roles;

using System;

public sealed record RenameRoleCommand(Guid RoleId, string Name);
