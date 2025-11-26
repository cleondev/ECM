using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecm.Sdk;

/// <summary>
/// Handles signing in to ECM using on-behalf authentication flows.
/// </summary>
public sealed class EcmOnBehalfAuthenticator
{
    private readonly IOptionsSnapshot<EcmIntegrationOptions> _options;
    private readonly ILogger<EcmOnBehalfAuthenticator> _logger;
    private readonly EcmSsoTokenProvider _ssoTokenProvider;
    private readonly SemaphoreSlim _signInMutex = new(1, 1);

    private bool _hasSignedIn;

    /// <summary>
    /// Initializes a new instance of the <see cref="EcmOnBehalfAuthenticator"/> class.
    /// </summary>
    /// <param name="options">Integration options that describe on-behalf behavior.</param>
    /// <param name="logger">Logger used to emit diagnostic messages.</param>
    /// <param name="ssoTokenProvider">Provider used to acquire SSO access tokens.</param>
    public EcmOnBehalfAuthenticator(
        IOptionsSnapshot<EcmIntegrationOptions> options,
        ILogger<EcmOnBehalfAuthenticator> logger,
        EcmSsoTokenProvider ssoTokenProvider)
    {
        _options = options;
        _logger = logger;
        _ssoTokenProvider = ssoTokenProvider;
    }

    /// <summary>
    /// Gets a value indicating whether on-behalf authentication is enabled.
    /// </summary>
    public bool IsEnabled => _options.Value.IsOnBehalfEnabled;

    /// <summary>
    /// Ensures the SDK is signed in when using on-behalf authentication.
    /// </summary>
    /// <param name="httpClient">HTTP client used to issue the sign-in request.</param>
    /// <param name="cancellationToken">Cancellation token used to abort the operation.</param>
    /// <returns>A task that completes when the sign-in flow has been executed.</returns>
    public async Task EnsureSignedInAsync(HttpClient httpClient, CancellationToken cancellationToken)
    {
        if (!IsEnabled)
        {
            return;
        }

        if (await TryAuthenticateWithSsoAsync(cancellationToken))
        {
            return;
        }

        if (!_options.Value.ApiKey.Enabled)
        {
            return;
        }

        await EnsureApiKeySignInAsync(httpClient, cancellationToken);
    }

    private async Task<bool> TryAuthenticateWithSsoAsync(CancellationToken cancellationToken)
    {
        if (!_options.Value.Sso.Enabled)
        {
            return false;
        }

        var token = await _ssoTokenProvider.GetAccessTokenAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning(
                "SSO is enabled but no access token is available. Falling back to API key on-behalf authentication when configured.");
            return false;
        }

        _logger.LogDebug("SSO access token acquired; skipping API key sign-in.");
        return true;
    }

    private async Task EnsureApiKeySignInAsync(HttpClient httpClient, CancellationToken cancellationToken)
    {
        if (_hasSignedIn)
        {
            return;
        }

        await _signInMutex.WaitAsync(cancellationToken);
        try
        {
            if (_hasSignedIn)
            {
                return;
            }

            var options = _options.Value;

            if (string.IsNullOrWhiteSpace(options.ApiKey.ApiKey))
            {
                throw new InvalidOperationException("Ecm:ApiKey:ApiKey must be configured when ApiKey.Enabled=true.");
            }

            if (string.IsNullOrWhiteSpace(options.OnBehalfUserEmail) && options.OnBehalfUserId is null)
            {
                throw new InvalidOperationException(
                    "Either Ecm:OnBehalfUserEmail or Ecm:OnBehalfUserId must be provided when ApiKey.Enabled=true.");
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/iam/auth/on-behalf")
            {
                Content = JsonContent.Create(new
                {
                    UserEmail = options.OnBehalfUserEmail,
                    UserId = options.OnBehalfUserId,
                }),
            };

            request.Headers.Add("X-Api-Key", options.ApiKey.ApiKey);

            using var response = await httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _hasSignedIn = true;
                _logger.LogInformation(
                    "Signed in via auth/on-behalf for {User}.",
                    options.OnBehalfUserEmail ?? options.OnBehalfUserId?.ToString());
                return;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogError(
                "Failed to sign in via auth/on-behalf. Status: {StatusCode}. Body: {Body}",
                response.StatusCode,
                string.IsNullOrWhiteSpace(body) ? "<empty>" : body);

            throw new HttpRequestException(
                $"On-behalf authentication failed with status {(int)response.StatusCode}: {body}");
        }
        finally
        {
            _signInMutex.Release();
        }
    }
}
