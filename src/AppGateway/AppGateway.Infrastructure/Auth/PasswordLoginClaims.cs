using System.Security.Claims;
using System.Text.Json;
using AppGateway.Contracts.IAM.Users;

namespace AppGateway.Infrastructure.Auth;

public static class PasswordLoginClaims
{
    public const string MarkerClaimType = "appgateway:password-login";
    public const string MarkerClaimValue = "true";
    public const string ProfileClaimType = "appgateway:password-login:profile";
    public const string OnBehalfClaimType = "on_behalf";

    public static bool IsPasswordLoginPrincipal(ClaimsPrincipal? principal)
        => principal?.HasClaim(MarkerClaimType, MarkerClaimValue) == true;

    public static UserSummaryDto? GetProfileFromPrincipal(
        ClaimsPrincipal? principal,
        out bool invalidProfileClaim)
    {
        invalidProfileClaim = false;

        if (principal is null)
        {
            return null;
        }

        var claim = principal.FindFirst(ProfileClaimType);
        if (claim is null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<UserSummaryDto>(claim.Value);
        }
        catch (JsonException)
        {
            invalidProfileClaim = true;
            return null;
        }
    }
}
