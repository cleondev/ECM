using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

using Ecm.Sdk.Configuration;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecm.Sdk.Authentication;

/// <summary>
/// Handles retrieving and caching ECM authentication sessions for individual users.
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
    /// Retrieves or fetches authentication material for the specified user.
    /// </summary>
    /// <param name="email">Email of the user requesting access.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Authentication session information for the requested user.</returns>
    public async Task<EcmAuthenticationSession> GetSessionForUserAsync(string email, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var cacheKey = string.Concat(TokenCachePrefix, email);

        var session = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(25);

            var authResponse = await RequestSessionAsync(email, cancellationToken);

            if (authResponse.ExpiresOn is { } expiresOn)
            {
                entry.AbsoluteExpiration = expiresOn;
            }
            else if (authResponse.ExpiresInMinutes is { } minutes && minutes > 0)
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(minutes);
            }

            return authResponse;
        });

        return session
            ?? throw new InvalidOperationException("Authentication API returned an empty token or cookie.");
    }

    private async Task<EcmAuthenticationSession> RequestSessionAsync(string email, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = _options.Value;

        if (string.IsNullOrWhiteSpace(options.ApiKey.ApiKey))
        {
            throw new InvalidOperationException("Ecm:ApiKey:ApiKey must be configured.");
        }

        var baseUri = new Uri(options.BaseUrl, UriKind.Absolute);

        if (_httpClient.BaseAddress != baseUri)
        {
            _httpClient.BaseAddress = baseUri;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/iam/auth/on-behalf")
        {
            Content = JsonContent.Create(new
            {
                userEmail = email,
                userId = (Guid?)null,
            }),
        };

        request.Headers.Add("X-Api-Key", options.ApiKey.ApiKey);

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

        var cookies = BuildCookieHeader(response);

        try
        {
            var session = ParseAuthenticationSession(content);

            session ??= new EcmAuthenticationSession(null, null, null, null);

            session = session with
            {
                CookieHeader = session.CookieHeader ?? cookies,
            };

            if (string.IsNullOrWhiteSpace(session.AccessToken) && string.IsNullOrWhiteSpace(session.CookieHeader))
            {
                throw new InvalidOperationException("Authentication API returned an empty token and no cookies.");
            }

            return session;
        }
        catch (JsonException exception)
        {
            _logger.LogError(exception, "Unable to parse authentication response body.");
            throw new InvalidOperationException("Unable to parse authentication response.", exception);
        }
    }

    private EcmAuthenticationSession? ParseAuthenticationSession(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;

        var token = TryGetString(root, "token") ?? TryGetString(root, "accessToken");
        var expiresInMinutes = TryGetInt32(root, "expiresInMinutes");
        var expiresOn = TryGetDateTimeOffset(root, "expiresOn");

        return new EcmAuthenticationSession(token, null, expiresOn, expiresInMinutes);
    }

    private static string? BuildCookieHeader(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            return null;
        }

        var cookies = setCookies
            .Select(value => value.Split(';', 2)[0].Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        return cookies.Length == 0 ? null : string.Join("; ", cookies);
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        return TryGetPropertyIgnoreCase(element, propertyName, out var property)
            && property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : null;
    }

    private static int? TryGetInt32(JsonElement element, string propertyName)
    {
        if (TryGetPropertyIgnoreCase(element, propertyName, out var property)
            && property.TryGetInt32(out var value))
        {
            return value;
        }

        return null;
    }

    private static DateTimeOffset? TryGetDateTimeOffset(JsonElement element, string propertyName)
    {
        if (TryGetPropertyIgnoreCase(element, propertyName, out var property))
        {
            if (property.ValueKind == JsonValueKind.String
                && DateTimeOffset.TryParse(property.GetString(), out var parsed))
            {
                return parsed;
            }

            if (property.ValueKind == JsonValueKind.Number
                && property.TryGetInt64(out var unixTime))
            {
                return DateTimeOffset.FromUnixTimeSeconds(unixTime);
            }
        }

        return null;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement property)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var candidate in element.EnumerateObject())
            {
                if (string.Equals(candidate.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    property = candidate.Value;
                    return true;
                }
            }
        }

        property = default;
        return false;
    }
}

public sealed record EcmAuthenticationSession(
    string? AccessToken,
    string? CookieHeader,
    DateTimeOffset? ExpiresOn,
    int? ExpiresInMinutes);
