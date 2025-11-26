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
    private readonly SemaphoreSlim _signInMutex = new(1, 1);

    private bool _hasSignedIn;

    /// <summary>
    /// Initializes a new instance of the <see cref="EcmOnBehalfAuthenticator"/> class.
    /// </summary>
    /// <param name="options">Integration options that describe on-behalf behavior.</param>
    /// <param name="logger">Logger used to emit diagnostic messages.</param>
    public EcmOnBehalfAuthenticator(
        IOptionsSnapshot<EcmIntegrationOptions> options,
        ILogger<EcmOnBehalfAuthenticator> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Gets a value indicating whether on-behalf authentication is enabled.
    /// </summary>
    public bool IsEnabled => _options.Value.OnBehalf.Enabled;

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

        if (_options.Value.OnBehalf.Sso.Enabled)
        {
            return;
        }

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

            var onBehalfOptions = _options.Value.OnBehalf;

            if (string.IsNullOrWhiteSpace(onBehalfOptions.ApiKey))
            {
                throw new InvalidOperationException("Ecm:OnBehalf:ApiKey must be configured when OnBehalf.Enabled=true.");
            }

            if (string.IsNullOrWhiteSpace(onBehalfOptions.UserEmail) && onBehalfOptions.UserId is null)
            {
                throw new InvalidOperationException(
                    "Either Ecm:OnBehalf:UserEmail or Ecm:OnBehalf:UserId must be provided when OnBehalf.Enabled=true.");
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/iam/auth/on-behalf")
            {
                Content = JsonContent.Create(new
                {
                    onBehalfOptions.UserEmail,
                    onBehalfOptions.UserId,
                }),
            };

            request.Headers.Add("X-Api-Key", onBehalfOptions.ApiKey);

            using var response = await httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _hasSignedIn = true;
                _logger.LogInformation(
                    "Signed in via auth/on-behalf for {User}.",
                    onBehalfOptions.UserEmail ?? onBehalfOptions.UserId?.ToString());
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
