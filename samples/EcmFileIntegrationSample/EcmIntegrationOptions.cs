namespace samples.EcmFileIntegrationSample;

public sealed class EcmIntegrationOptions
{
    public string BaseUrl { get; set; } = string.Empty;

    public string? AccessToken { get; set; }

    public Guid? OwnerId { get; set; }

    public Guid? CreatedBy { get; set; }

    public string FilePath { get; set; } = "sample-data/hello-world.txt";

    public string DocType { get; set; } = "General";

    public string Status { get; set; } = "Draft";

    public string Sensitivity { get; set; } = "Internal";

    public Guid? DocumentTypeId { get; set; }

    public string? Title { get; set; }
}
