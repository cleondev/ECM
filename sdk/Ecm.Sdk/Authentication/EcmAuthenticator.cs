using System.Net.Http.Json;
using System.Text.Json;

using Ecm.Sdk.Configuration;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecm.Sdk.Authentication;

/// <summary>
/// Handles retrieving and caching ECM access tokens for individual users.
/// </summary>
public sealed class EcmAuthenticator(
    HttpClient httpClient,
    IOptionsSnapshot<EcmIntegrationOptions> options,
    IMemoryCache cache,
    ILogger<EcmAuthenticator> logger)
{
    private const string TokenCachePrefix = "ecm_token_";

    private readonly HttpClient _httpClient = httpClient;
    private readonly IOptionsSnapshot<EcmIntegrationOptions> _options = options;
    private readonly IMemoryCache _cache = cache;
    private readonly ILogger<EcmAuthenticator> _logger = logger;

    /// <summary>
    /// Retrieves or fetches a token for the specified user.
    /// </summary>
    /// <param name="email">Email of the user requesting access.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The bearer token for the requested user.</returns>
    public async Task<string> GetTokenForUserAsync(string email, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var cacheKey = string.Concat(TokenCachePrefix, email);

        var token = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(25);

            var authResponse = await RequestTokenAsync(email, cancellationToken);

            if (authResponse.ExpiresInMinutes is { } minutes && minutes > 0)
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(minutes);
            }

            return authResponse.Token
                ?? authResponse.AccessToken
                ?? throw new InvalidOperationException("Authentication API returned an empty token.");
        });

        return token ?? throw new InvalidOperationException("Authentication API returned an empty token.");
    }

    private async Task<AuthenticateResponse> RequestTokenAsync(string email, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = _options.Value;

        if (string.IsNullOrWhiteSpace(options.ApiKey.ApiKey))
        {
            throw new InvalidOperationException("Ecm:ApiKey:ApiKey must be configured.");
        }

        var baseUri = new Uri(options.BaseUrl);

        if (_httpClient.BaseAddress != baseUri)
        {
            _httpClient.BaseAddress = baseUri;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/iam/on-behalf")
        {
            Content = JsonContent.Create(new
            {
                apiKey = options.ApiKey.ApiKey,
                userEmail = email,
                userId = (Guid?)null,
            }),
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Authentication request failed with status {StatusCode}. Body: {Body}",
                response.StatusCode,
                string.IsNullOrWhiteSpace(content) ? "<empty>" : content);

            throw new HttpRequestException(
                $"Authentication request failed with status {(int)response.StatusCode}: {content}");
        }

        try
        {
            return JsonSerializer.Deserialize<AuthenticateResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            }) ?? new AuthenticateResponse(null, null);
        }
        catch (JsonException exception)
        {
            _logger.LogError(exception, "Unable to parse authentication response body.");
            throw new InvalidOperationException("Unable to parse authentication response.", exception);
        }
    }

    private sealed record AuthenticateResponse(string? Token, int? ExpiresInMinutes)
    {
        public string? AccessToken { get; init; }
    }
}
