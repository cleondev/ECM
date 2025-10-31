using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;

using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace AppGateway.Infrastructure.Ecm;

internal sealed class EcmShareAccessClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor,
    ITokenAcquisition tokenAcquisition,
    IOptions<EcmApiClientOptions> options,
    ILogger<EcmShareAccessClient> logger) : IEcmShareAccessClient
{
    private const string HomeAccountIdClaimType = "homeAccountId";

    private readonly HttpClient _httpClient = httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ITokenAcquisition _tokenAcquisition = tokenAcquisition;
    private readonly EcmApiClientOptions _options = options.Value;
    private readonly ILogger<EcmShareAccessClient> _logger = logger;

    public async Task<HttpResponseMessage> GetInterstitialAsync(
        string code,
        string? password,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var path = BuildPath($"s/{Uri.EscapeDataString(code)}", password);
        var request = await CreateRequestAsync(HttpMethod.Get, path, cancellationToken);

        return await _httpClient.SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> VerifyPasswordAsync(
        string code,
        string password,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var request = await CreateRequestAsync(
            HttpMethod.Post,
            $"s/{Uri.EscapeDataString(code)}/password",
            cancellationToken);
        request.Content = JsonContent.Create(new { password });

        return await _httpClient.SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> CreatePresignedUrlAsync(
        string code,
        string? password,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var request = await CreateRequestAsync(
            HttpMethod.Post,
            $"s/{Uri.EscapeDataString(code)}/presign",
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(password))
        {
            request.Content = JsonContent.Create(new { password });
        }
        else
        {
            request.Content = JsonContent.Create(new { });
        }

        return await _httpClient.SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> RedirectToDownloadAsync(
        string code,
        string? password,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var path = BuildPath($"s/{Uri.EscapeDataString(code)}/download", password);
        var request = await CreateRequestAsync(HttpMethod.Get, path, cancellationToken);

        return await _httpClient.SendAsync(request, cancellationToken);
    }

    private static string BuildPath(string path, string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return path;
        }

        return QueryHelpers.AddQueryString(path, "password", password);
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(
        HttpMethod method,
        string path,
        CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        await AttachAuthenticationAsync(request, cancellationToken);
        ApplyForwardedHeaders(request);
        return request;
    }

    private async Task AttachAuthenticationAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var authorization = httpContext?.Request.Headers.Authorization;
        if (!string.IsNullOrWhiteSpace(authorization))
        {
            request.Headers.TryAddWithoutValidation("Authorization", authorization.ToString());
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.Scope))
        {
            return;
        }

        var scopes = ScopeUtilities.ParseScopes(_options.Scope);
        if (scopes.Length == 0)
        {
            return;
        }

        var principal = EnsureHomeAccountIdentifiers(httpContext?.User);
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var authenticationScheme = string.IsNullOrWhiteSpace(_options.AuthenticationScheme)
            ? OpenIdConnectDefaults.AuthenticationScheme
            : _options.AuthenticationScheme;

        try
        {
            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(
                scopes,
                authenticationScheme: authenticationScheme,
                tenantId: _options.TenantId,
                user: principal,
                tokenAcquisitionOptions: new TokenAcquisitionOptions
                {
                    CancellationToken = cancellationToken,
                });

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
        catch (Exception exception) when (exception is MsalException or MicrosoftIdentityWebChallengeUserException)
        {
            _logger.LogWarning(
                exception,
                "Unable to acquire user access token for share access request.");
        }
    }

    private void ApplyForwardedHeaders(HttpRequestMessage request)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return;
        }

        var origin = httpContext.Request;

        if (!string.IsNullOrWhiteSpace(origin.Scheme))
        {
            request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", origin.Scheme);
        }

        if (origin.Host.HasValue)
        {
            request.Headers.TryAddWithoutValidation("X-Forwarded-Host", origin.Host.Value);

            if (origin.Host.Port is > 0)
            {
                request.Headers.TryAddWithoutValidation(
                    "X-Forwarded-Port",
                    origin.Host.Port.Value.ToString(CultureInfo.InvariantCulture));
            }
        }

        if (origin.PathBase.HasValue && origin.PathBase != PathString.Empty)
        {
            request.Headers.TryAddWithoutValidation("X-Forwarded-Prefix", origin.PathBase.Value);
        }
    }

    private static ClaimsPrincipal? EnsureHomeAccountIdentifiers(ClaimsPrincipal? principal)
    {
        if (principal?.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return principal;
        }

        if (string.IsNullOrWhiteSpace(principal.GetHomeObjectId()))
        {
            var objectId = principal.GetObjectId();
            if (!string.IsNullOrWhiteSpace(objectId) && !identity.HasClaim(c => c.Type == ClaimConstants.UniqueObjectIdentifier))
            {
                identity.AddClaim(new Claim(ClaimConstants.UniqueObjectIdentifier, objectId));
            }
        }

        if (string.IsNullOrWhiteSpace(principal.GetHomeTenantId()))
        {
            var tenantId = principal.GetTenantId();
            if (!string.IsNullOrWhiteSpace(tenantId) && !identity.HasClaim(c => c.Type == ClaimConstants.UniqueTenantIdentifier))
            {
                identity.AddClaim(new Claim(ClaimConstants.UniqueTenantIdentifier, tenantId));
            }
        }

        if (string.IsNullOrWhiteSpace(principal.FindFirstValue(HomeAccountIdClaimType)))
        {
            var objectId = principal.GetObjectId();
            var tenantId = principal.GetTenantId();

            if (!string.IsNullOrWhiteSpace(objectId)
                && !string.IsNullOrWhiteSpace(tenantId)
                && !identity.HasClaim(c => c.Type == HomeAccountIdClaimType))
            {
                identity.AddClaim(new Claim(HomeAccountIdClaimType, $"{objectId}.{tenantId}"));
            }
        }

        return principal;
    }
}
