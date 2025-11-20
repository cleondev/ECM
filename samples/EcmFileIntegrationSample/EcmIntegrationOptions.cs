namespace samples.EcmFileIntegrationSample;

public sealed class EcmIntegrationOptions
{
    public string BaseUrl { get; set; } = string.Empty;

    public string? AccessToken { get; set; }

    public Guid? OwnerId { get; set; }

    public Guid? CreatedBy { get; set; }

    public string DocType { get; set; } = "General";

    public string Status { get; set; } = "Draft";

    public string Sensitivity { get; set; } = "Internal";

    public Guid? DocumentTypeId { get; set; }

    public string? Title { get; set; }

    public bool UseAzureSso { get; set; }

    public string? AuthenticationScope { get; set; }
}
