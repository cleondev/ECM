namespace AppGateway.Contracts.AccessControl.Users;

using System;

public sealed class AssignRoleRequestDto
{
    public Guid RoleId { get; init; }
}
