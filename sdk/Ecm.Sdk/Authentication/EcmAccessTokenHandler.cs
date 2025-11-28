using System;
using System.Net.Http.Headers;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Ecm.Sdk.Authentication;

/// <summary>
/// HTTP message handler that attaches ECM access tokens and logs transient errors.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EcmAccessTokenHandler"/> class.
/// </remarks>
/// <param name="authenticator">Authenticator responsible for retrieving user tokens.</param>
/// <param name="httpContextAccessor">Accessor used to resolve the current HTTP context.</param>
/// <param name="logger">Logger used for request diagnostics.</param>
public sealed class EcmAccessTokenHandler(
    EcmAuthenticator authenticator,
    IHttpContextAccessor httpContextAccessor,
    ILogger<EcmAccessTokenHandler> logger) : DelegatingHandler
{
    private readonly EcmAuthenticator _authenticator = authenticator;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
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
            ?? throw new Exception("Missing user identity");

        var incomingAuthorization = httpContext.Request.Headers.Authorization;

        if (!StringValues.IsNullOrEmpty(incomingAuthorization)
            && AuthenticationHeaderValue.TryParse(incomingAuthorization, out var parsedAuthorization)
            && string.Equals(parsedAuthorization.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.Authorization = parsedAuthorization;
            return await base.SendAsync(request, cancellationToken);
        }

        var email = httpContext.User.FindFirst("email")?.Value
            ?? httpContext.User.Identity?.Name
            ?? throw new Exception("Missing user identity");

        var token = await _authenticator.GetTokenForUserAsync(email, cancellationToken);

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(exception, "Request to {Url} failed.", request.RequestUri);
            throw;
        }
    }
}
