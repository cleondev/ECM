namespace ECM.Document.Api.Documents;

public sealed class UpdateDocumentRequest
{
    public string? Title { get; init; }

    public string? Status { get; init; }

    public string? Sensitivity { get; init; }

    public string? Department { get; init; }
}
