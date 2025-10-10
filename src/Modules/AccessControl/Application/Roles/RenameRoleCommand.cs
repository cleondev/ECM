namespace ECM.AccessControl.Application.Roles;

using System;

public sealed record RenameRoleCommand(Guid RoleId, string Name);
