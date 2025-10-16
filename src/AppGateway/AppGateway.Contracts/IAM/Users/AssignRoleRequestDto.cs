namespace AppGateway.Contracts.IAM.Users;

using System;

public sealed class AssignRoleRequestDto
{
    public Guid RoleId { get; init; }
}
