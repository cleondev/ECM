using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecm.Sdk;

public sealed class EcmAccessTokenProvider
{
    private readonly IOptions<EcmIntegrationOptions> _options;
    private readonly ILogger<EcmAccessTokenProvider> _logger;
    private readonly EcmSsoTokenProvider _ssoTokenProvider;

    public EcmAccessTokenProvider(
        IOptions<EcmIntegrationOptions> options,
        ILogger<EcmAccessTokenProvider> logger,
        EcmSsoTokenProvider ssoTokenProvider)
    {
        _options = options;
        _logger = logger;
        _ssoTokenProvider = ssoTokenProvider;
    }

    public bool HasConfiguredAccess => !string.IsNullOrWhiteSpace(_options.Value.AccessToken)
        || _options.Value.OnBehalf.Enabled;

    public bool UsingOnBehalfAuthentication => _options.Value.OnBehalf.Enabled;

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = _options.Value;

        if (options.OnBehalf.Sso.Enabled)
        {
            var ssoToken = await _ssoTokenProvider.GetAccessTokenAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(ssoToken))
            {
                return ssoToken;
            }
        }

        if (options.OnBehalf.Enabled && !options.OnBehalf.Sso.Enabled)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(options.AccessToken))
        {
            return options.AccessToken;
        }

        _logger.LogWarning("Không tìm thấy AccessToken và OnBehalf chưa được bật. Request sẽ không có bearer token.");
        return null;
    }
}
