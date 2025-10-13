namespace ECM.AccessControl.Api.Users;

public sealed class UpdateUserProfileRequest
{
    public string DisplayName { get; set; } = string.Empty;

    public string? Department { get; set; }
}
