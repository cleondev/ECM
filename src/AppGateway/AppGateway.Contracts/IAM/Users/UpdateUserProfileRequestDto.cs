namespace AppGateway.Contracts.IAM.Users;

public sealed class UpdateUserProfileRequestDto
{
    public string DisplayName { get; set; } = string.Empty;

    public string? Department { get; set; }
}
