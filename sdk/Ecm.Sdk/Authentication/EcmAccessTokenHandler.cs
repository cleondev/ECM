using System.Net.Http.Headers;

using Ecm.Sdk.Configuration;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Ecm.Sdk.Authentication;

/// <summary>
/// HTTP message handler that attaches ECM authentication credentials and logs transient errors.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EcmAccessTokenHandler"/> class.
/// </remarks>
/// <param name="authenticator">Authenticator responsible for retrieving user tokens.</param>
/// <param name="httpContextAccessor">Accessor used to resolve the current HTTP context.</param>
/// <param name="options">Integration options controlling SSO and API key behavior.</param>
/// <param name="logger">Logger used for request diagnostics.</param>
public sealed class EcmAccessTokenHandler(
    EcmAuthenticator authenticator,
    IHttpContextAccessor httpContextAccessor,
    IOptions<EcmIntegrationOptions> options,
    ILogger<EcmAccessTokenHandler> logger) : DelegatingHandler
{
    private readonly EcmAuthenticator _authenticator = authenticator;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly EcmIntegrationOptions _options = options.Value;
    private readonly ILogger<EcmAccessTokenHandler> _logger = logger;

    /// <summary>
    /// Adds the bearer token to outgoing requests and delegates execution to the next handler.
    /// </summary>
    /// <param name="request">The HTTP request to send.</param>
    /// <param name="cancellationToken">Cancellation token used to abort the request.</param>
    /// <returns>The HTTP response returned by the pipeline.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new ArgumentException("Missing user identity");
        if (_options.Sso.Enabled)
        {
            var incomingAuthorization = httpContext.Request.Headers["Authorization"];

            if (!StringValues.IsNullOrEmpty(incomingAuthorization)
                && AuthenticationHeaderValue.TryParse(incomingAuthorization, out var parsedAuthorization)
                && string.Equals(parsedAuthorization.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase))
            {
                request.Headers.Authorization = parsedAuthorization;
                try
                {
                    return await base.SendAsync(request, cancellationToken);
                }
                catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
                {
                    _logger.LogError(exception, "Request to {Url} failed.", request.RequestUri);

                    throw new HttpRequestException(
                        $"Request to {request.RequestUri} failed.",
                        exception);
                }
            }
        }

        var email = httpContext.User.FindFirst("email")?.Value
            ?? httpContext.User.Identity?.Name
            ?? throw new ArgumentException("Missing user identity");

        var session = await _authenticator.GetSessionForUserAsync(email, cancellationToken);

        if (!string.IsNullOrWhiteSpace(session.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        }

        if (!string.IsNullOrWhiteSpace(session.CookieHeader))
        {
            request.Headers.Add("Cookie", session.CookieHeader);
        }

        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(exception, "Request to {Url} failed.", request.RequestUri);

            throw new HttpRequestException(
                $"Request to {request.RequestUri} failed.",
                exception);
        }
    }
}
