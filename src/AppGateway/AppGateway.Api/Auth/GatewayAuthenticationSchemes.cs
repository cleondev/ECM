using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace AppGateway.Api.Auth;

public static class GatewayAuthenticationSchemes
{
    public const string Default = string.Join(",", new[]
    {
        JwtBearerDefaults.AuthenticationScheme,
        CookieAuthenticationDefaults.AuthenticationScheme,
        ApiKeyAuthenticationHandler.AuthenticationScheme
    });
}
