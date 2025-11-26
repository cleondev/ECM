using System.Net.Http.Headers;

using Microsoft.Extensions.Logging;

namespace Ecm.Sdk.Authentication;

/// <summary>
/// HTTP message handler that attaches ECM access tokens and logs transient errors.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EcmAccessTokenHandler"/> class.
/// </remarks>
/// <param name="provider">Provider used to resolve access tokens.</param>
/// <param name="logger">Logger used for request diagnostics.</param>
public sealed class EcmAccessTokenHandler(EcmAccessTokenProvider provider, ILogger<EcmAccessTokenHandler> logger) : DelegatingHandler
{
    private readonly EcmAccessTokenProvider _provider = provider;
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
        var token = await _provider.GetAccessTokenAsync(cancellationToken);

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
