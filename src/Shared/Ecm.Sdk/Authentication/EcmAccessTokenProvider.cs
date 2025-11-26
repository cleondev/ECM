using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecm.Sdk;

/// <summary>
/// Provides access tokens for communicating with the ECM APIs.
/// </summary>
public sealed class EcmAccessTokenProvider
{
    private readonly IOptionsSnapshot<EcmIntegrationOptions> _options;
    private readonly ILogger<EcmAccessTokenProvider> _logger;
    private readonly EcmSsoTokenProvider _ssoTokenProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EcmAccessTokenProvider"/> class.
    /// </summary>
    /// <param name="options">Integration options containing authentication settings.</param>
    /// <param name="logger">Logger used to emit diagnostic messages.</param>
    /// <param name="ssoTokenProvider">Provider used to retrieve delegated SSO tokens.</param>
    public EcmAccessTokenProvider(
        IOptionsSnapshot<EcmIntegrationOptions> options,
        ILogger<EcmAccessTokenProvider> logger,
        EcmSsoTokenProvider ssoTokenProvider)
    {
        _options = options;
        _logger = logger;
        _ssoTokenProvider = ssoTokenProvider;
    }

    /// <summary>
    /// Gets a value indicating whether any authentication method has been configured.
    /// </summary>
    public bool HasConfiguredAccess => _options.Value.ApiKey.Enabled || _options.Value.Sso.Enabled;

    /// <summary>
    /// Gets a value indicating whether on-behalf authentication is configured.
    /// </summary>
    public bool UsingOnBehalfAuthentication => _options.Value.IsOnBehalfEnabled;

    /// <summary>
    /// Resolves an access token using either a configured static token or on-behalf SSO flow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token used to cancel the retrieval operation.</param>
    /// <returns>A bearer token string when available; otherwise, <c>null</c>.</returns>
    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = _options.Value;

        if (options.Sso.Enabled)
        {
            var ssoToken = await _ssoTokenProvider.GetAccessTokenAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(ssoToken))
            {
                return ssoToken;
            }
        }

        if (options.ApiKey.Enabled)
        {
            return null;
        }

        _logger.LogWarning("Chưa cấu hình ApiKey hoặc SSO cho on-behalf. Request sẽ không có bearer token.");
        return null;
    }
}
