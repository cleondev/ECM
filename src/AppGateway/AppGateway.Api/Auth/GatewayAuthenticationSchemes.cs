using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace AppGateway.Api.Auth;

public static class GatewayAuthenticationSchemes
{
    public const string Default = $"{JwtBearerDefaults.AuthenticationScheme},{CookieAuthenticationDefaults.AuthenticationScheme},{ApiKeyAuthenticationHandler.AuthenticationScheme}";
}
