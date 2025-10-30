namespace AppGateway.Contracts.IAM.Users;

using System.Text.Json.Serialization;

public sealed class UpdateUserPasswordRequestDto
{
    [JsonPropertyName("currentPassword")]
    public string? CurrentPassword { get; init; }

    [JsonPropertyName("newPassword")]
    public string NewPassword { get; init; } = string.Empty;
}
