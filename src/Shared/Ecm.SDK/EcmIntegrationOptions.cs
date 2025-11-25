namespace Ecm.Sdk;

public sealed class EcmIntegrationOptions
{
    public string BaseUrl { get; set; } = string.Empty;

    public string? AccessToken { get; set; }

    public OnBehalfOptions OnBehalf { get; set; } = new();

    public Guid? OwnerId { get; set; }

    public Guid? CreatedBy { get; set; }

    public string DocType { get; set; } = "General";

    public string Status { get; set; } = "Draft";

    public string Sensitivity { get; set; } = "Internal";

    public Guid? DocumentTypeId { get; set; }

    public string? Title { get; set; }
}

public sealed class OnBehalfOptions
{
    public bool Enabled { get; set; }

    public string? ApiKey { get; set; }

    public Guid? UserId { get; set; }

    public string? UserEmail { get; set; }

    public SsoOptions Sso { get; set; } = new();
}

public sealed class SsoOptions
{
    public bool Enabled { get; set; }

    public string? Authority { get; set; }

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public string[] Scopes { get; set; } = Array.Empty<string>();

    public string? UserAccessToken { get; set; }
}
