namespace Ecm.Sdk.Configuration;

/// <summary>
/// Configures the ECM SDK integration details used to connect to the ECM APIs.
/// </summary>
public sealed class EcmIntegrationOptions
{
    /// <summary>
    /// Base address of the ECM API endpoint (AppGateway).
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// API key based authentication settings for ECM access.
    /// Used when SSO is disabled or when no upstream bearer token is present.
    /// </summary>
    public ApiKeyOptions ApiKey { get; set; } = new();

    /// <summary>
    /// Single sign-on settings used when forwarding user tokens directly to AppGateway.
    /// When <see cref="SsoOptions.Enabled"/> is true and a Bearer token exists on the incoming request,
    /// the SDK will forward that token as-is instead of using the API key flow.
    /// </summary>
    public SsoOptions Sso { get; set; } = new();
}

/// <summary>
/// Options controlling API key based authentication.
/// </summary>
public sealed class ApiKeyOptions
{
    /// <summary>
    /// Indicates whether API key authentication is enabled.
    /// This flag is mostly informational; the SDK will fall back to API key
    /// whenever SSO is disabled or no upstream bearer token is available.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// API key used to authenticate requests for access tokens.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Default user identity (email) used when no <see cref="IEcmUserContext"/>
    /// is available, such as in console/worker environments without HTTP context.
    /// </summary>
    public string? DefaultUserEmail { get; set; }
}

/// <summary>
/// Options used for single sign-on integration.
/// </summary>
public sealed class SsoOptions
{
    /// <summary>
    /// Indicates whether SSO-based token forwarding is enabled.
    /// When enabled and the current HTTP request contains a valid Bearer token,
    /// the SDK will forward that token directly to AppGateway.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Authority URL of the identity provider issuing tokens (e.g. Azure AD tenant URL).
    /// This is primarily used by the hosting application to acquire tokens; the SDK itself
    /// only forwards the incoming bearer token.
    /// </summary>
    public string? Authority { get; set; }

    /// <summary>
    /// Client identifier registered with the identity provider.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Client secret used to authenticate with the identity provider.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// API scopes requested during token acquisition by the hosting application.
    /// </summary>
    public string[] Scopes { get; set; } = Array.Empty<string>();
}
