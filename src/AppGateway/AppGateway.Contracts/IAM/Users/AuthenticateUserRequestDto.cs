namespace AppGateway.Contracts.IAM.Users;

public sealed class AuthenticateUserRequestDto
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
