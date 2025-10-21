using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace AppGateway.Infrastructure.Ecm;

public sealed class EcmApiClientOptions
{
    public string? Scope { get; set; }

    public string AuthenticationScheme { get; set; } = OpenIdConnectDefaults.AuthenticationScheme;
}
