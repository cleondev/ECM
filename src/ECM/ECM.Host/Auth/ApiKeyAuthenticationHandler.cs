using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECM.Host.Auth;

internal sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    IOptionsMonitor<ApiKeyOptions> apiKeyOptions,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string AuthenticationScheme = "ApiKey";
    public const string HeaderName = "X-Api-Key";

    private readonly IOptionsMonitor<ApiKeyOptions> _apiKeyOptions = apiKeyOptions;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var headerValue))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing API key."));
        }

        var options = _apiKeyOptions.CurrentValue;
        if (options.Keys.Count == 0)
        {
            return Task.FromResult(AuthenticateResult.Fail("API key validation is not configured."));
        }

        var providedKey = headerValue.ToString();
        var matchedKey = options.Keys.FirstOrDefault(
            key => string.Equals(key.Key?.Trim(), providedKey, StringComparison.Ordinal));

        if (matchedKey is null)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        var displayName = string.IsNullOrWhiteSpace(matchedKey.Name)
            ? "Gateway Client"
            : matchedKey.Name.Trim();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "gateway-client"),
            new Claim(ClaimTypes.Name, displayName)
        };

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
