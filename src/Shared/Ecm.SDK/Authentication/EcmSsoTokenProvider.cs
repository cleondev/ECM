using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace Ecm.Sdk;

public sealed class EcmSsoTokenProvider
{
    private readonly IOptions<EcmIntegrationOptions> _options;
    private readonly ILogger<EcmSsoTokenProvider> _logger;
    private readonly SemaphoreSlim _mutex = new(1, 1);

    private AuthenticationResult? _cachedToken;

    public EcmSsoTokenProvider(IOptions<EcmIntegrationOptions> options, ILogger<EcmSsoTokenProvider> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var ssoOptions = _options.Value.OnBehalf.Sso;
        if (!ssoOptions.Enabled)
        {
            return null;
        }

        if (_cachedToken is { ExpiresOn: var expiry } && expiry > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return _cachedToken.AccessToken;
        }

        await _mutex.WaitAsync(cancellationToken);
        try
        {
            if (_cachedToken is { ExpiresOn: var refreshedExpiry } && refreshedExpiry > DateTimeOffset.UtcNow.AddMinutes(1))
            {
                return _cachedToken.AccessToken;
            }

            if (string.IsNullOrWhiteSpace(ssoOptions.UserAccessToken))
            {
                _logger.LogWarning("On-behalf SSO được bật nhưng chưa truyền UserAccessToken.");
                return null;
            }

            var app = ConfidentialClientApplicationBuilder
                .Create(ssoOptions.ClientId!)
                .WithAuthority(ssoOptions.Authority!)
                .WithClientSecret(ssoOptions.ClientSecret!)
                .Build();

            var result = await app
                .AcquireTokenOnBehalfOf(ssoOptions.Scopes, new UserAssertion(ssoOptions.UserAccessToken))
                .ExecuteAsync(cancellationToken);

            _cachedToken = result;
            return result.AccessToken;
        }
        catch (MsalServiceException exception)
        {
            _logger.LogError(exception, "Không lấy được token OBO qua SSO: {Message}", exception.Message);
            return null;
        }
        finally
        {
            _mutex.Release();
        }
    }
}
