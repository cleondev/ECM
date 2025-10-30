namespace AppGateway.Contracts.IAM.Roles;

public sealed class CreateRoleRequestDto
{
    public string Name { get; init; } = string.Empty;
}
