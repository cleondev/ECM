using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecm.FileIntegration;

public sealed class EcmAccessTokenProvider
{
    private readonly IOptions<EcmIntegrationOptions> _options;
    private readonly ILogger<EcmAccessTokenProvider> _logger;

    public EcmAccessTokenProvider(
        IOptions<EcmIntegrationOptions> options,
        ILogger<EcmAccessTokenProvider> logger)
    {
        _options = options;
        _logger = logger;
    }

    public bool HasConfiguredAccess => !string.IsNullOrWhiteSpace(_options.Value.AccessToken)
        || _options.Value.OnBehalf.Enabled;

    public bool UsingOnBehalfAuthentication => _options.Value.OnBehalf.Enabled;

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = _options.Value;

        if (options.OnBehalf.Enabled)
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
