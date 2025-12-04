namespace Shared.Extensions.Http;

/// <summary>
/// Header names used to propagate password-login user context from the App Gateway to downstream services.
/// </summary>
public static class PasswordLoginForwardingHeaders
{
    public const string UserId = "X-Password-Login-UserId";
    public const string Email = "X-Password-Login-Email";
    public const string DisplayName = "X-Password-Login-DisplayName";
    public const string PreferredUsername = "X-Password-Login-PreferredUsername";
    public const string PrimaryGroupId = "X-Password-Login-Primary-GroupId";
    public const string PrimaryGroupName = "X-Password-Login-Primary-Group-Name";
    public const string OnBehalf = "X-Password-Login-On-Behalf";
    public const string Roles = "X-Password-Login-Roles";
}
