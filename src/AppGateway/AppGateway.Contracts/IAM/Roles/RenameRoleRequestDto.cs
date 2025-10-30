namespace AppGateway.Contracts.IAM.Roles;

public sealed class RenameRoleRequestDto
{
    public string Name { get; init; } = string.Empty;
}
