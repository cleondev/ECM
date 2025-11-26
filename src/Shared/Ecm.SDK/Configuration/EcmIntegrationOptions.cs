namespace Ecm.Sdk;

/// <summary>
/// Configures the ECM SDK integration details used to connect to the ECM APIs.
/// </summary>
public sealed class EcmIntegrationOptions
{
    /// <summary>
    /// Base address of the ECM API endpoint.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Static bearer token used for direct authentication when on-behalf is disabled.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Settings for authenticating and acting on behalf of another user.
    /// </summary>
    public OnBehalfOptions OnBehalf { get; set; } = new();

    /// <summary>
    /// Identifier of the document owner when uploading new content.
    /// </summary>
    public Guid? OwnerId { get; set; }

    /// <summary>
    /// Identifier of the user creating the document when uploading.
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// Default document type used during uploads.
    /// </summary>
    public string DocType { get; set; } = "General";

    /// <summary>
    /// Default status applied to uploaded documents.
    /// </summary>
    public string Status { get; set; } = "Draft";

    /// <summary>
    /// Default sensitivity label applied to uploads.
    /// </summary>
    public string Sensitivity { get; set; } = "Internal";

    /// <summary>
    /// Optional identifier for the document type schema when configured.
    /// </summary>
    public Guid? DocumentTypeId { get; set; }

    /// <summary>
    /// Optional title applied to uploads when provided.
    /// </summary>
    public string? Title { get; set; }
}

/// <summary>
/// Options controlling how the SDK performs on-behalf authentication.
/// </summary>
public sealed class OnBehalfOptions
{
    /// <summary>
    /// Indicates whether on-behalf authentication is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// API key used to authenticate the on-behalf sign-in request.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Identifier of the user to impersonate when on-behalf authentication is enabled.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Email of the user to impersonate when on-behalf authentication is enabled.
    /// </summary>
    public string? UserEmail { get; set; }

    /// <summary>
    /// Settings for acquiring tokens via SSO when acting on behalf of a user.
    /// </summary>
    public SsoOptions Sso { get; set; } = new();
}

/// <summary>
/// Options used for single sign-on token acquisition when performing on-behalf flows.
/// </summary>
public sealed class SsoOptions
{
    /// <summary>
    /// Indicates whether SSO-based on-behalf token acquisition is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Authority URL of the identity provider issuing tokens.
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
    /// API scopes requested during token acquisition.
    /// </summary>
    public string[] Scopes { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Access token received from the calling user that is exchanged for a delegated token.
    /// </summary>
    public string? UserAccessToken { get; set; }
}
