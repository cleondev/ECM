using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecm.FileIntegration;

public sealed class EcmOnBehalfAuthenticator
{
    private readonly IOptions<EcmIntegrationOptions> _options;
    private readonly ILogger<EcmOnBehalfAuthenticator> _logger;
    private readonly SemaphoreSlim _signInMutex = new(1, 1);

    private bool _hasSignedIn;

    public EcmOnBehalfAuthenticator(
        IOptions<EcmIntegrationOptions> options,
        ILogger<EcmOnBehalfAuthenticator> logger)
    {
        _options = options;
        _logger = logger;
    }

    public bool IsEnabled => _options.Value.OnBehalf.Enabled;

    public async Task EnsureSignedInAsync(HttpClient httpClient, CancellationToken cancellationToken)
    {
        if (!IsEnabled)
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
