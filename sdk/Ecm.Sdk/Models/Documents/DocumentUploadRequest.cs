namespace Ecm.Sdk.Models.Documents;

/// <summary>
/// Represents the information required to upload a document to ECM.
/// </summary>
public sealed record DocumentUploadRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentUploadRequest"/> class.
    /// </summary>>
    public DocumentUploadRequest()
    {
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentUploadRequest"/> class.
    /// </summary>
    /// <param name="docType">Document type key used by the ECM service.</param>
    /// <param name="status">Initial lifecycle status of the document.</param>
    /// <param name="sensitivity">Sensitivity level label associated with the document.</param>
    /// <param name="filePath">Absolute or relative path to the file being uploaded.</param>
    public DocumentUploadRequest(string docType, string status, string sensitivity, string filePath)
    {
        DocType = docType;
        Status = status;
        Sensitivity = sensitivity;
    }

    /// <summary>
    /// Identifier of the owning user or group in ECM.
    /// </summary>
    public Guid OwnerId { get; init; }

    /// <summary>
    /// Identifier of the user creating the document record.
    /// </summary>
    public Guid CreatedBy { get; init; }

    /// <summary>
    /// Document type key that determines the storage schema in ECM.
    /// </summary>
    public string DocType { get; init; } = "General";

    /// <summary>
    /// Initial workflow status applied to the document.
    /// </summary>
    public string Status { get; init; } = "Draft";

    /// <summary>
    /// Sensitivity label used to classify the document.
    /// </summary>
    public string Sensitivity { get; init; } = "Internal";

    /// <summary>
    /// File steam containing the document content to be uploaded.
    /// </summary>
    public Stream FileContent { get; set; } = Stream.Null;

    /// <summary>
    /// Optional identifier of the document type when the type key is insufficient.
    /// </summary>
    public Guid? DocumentTypeId { get; init; }

    /// <summary>
    /// Optional title that overrides the file name when present.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Optional original file name preserved for downstream consumers.
    /// </summary>
    public string? FileName { get; init; }

    /// <summary>
    /// Optional MIME type sent to ECM when it cannot be inferred from the file.
    /// </summary>
    public string? ContentType { get; init; }
}
