namespace ECM.IAM.Api.Auth;

public sealed class AuthenticateUserRequest
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
