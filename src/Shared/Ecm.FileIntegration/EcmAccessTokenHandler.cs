using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace Ecm.FileIntegration;

public sealed class EcmAccessTokenHandler : DelegatingHandler
{
    private readonly EcmAccessTokenProvider _provider;
    private readonly ILogger<EcmAccessTokenHandler> _logger;

    public EcmAccessTokenHandler(EcmAccessTokenProvider provider, ILogger<EcmAccessTokenHandler> logger)
    {
        _provider = provider;
        _logger = logger;
    }

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
