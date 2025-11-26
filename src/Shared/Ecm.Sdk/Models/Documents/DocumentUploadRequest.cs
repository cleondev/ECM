namespace Ecm.Sdk;

/// <summary>
/// Represents the information required to upload a document to ECM.
/// </summary>
public sealed record DocumentUploadRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentUploadRequest"/> class.
    /// </summary>
    /// <param name="ownerId">Identifier of the owning user or group.</param>
    /// <param name="createdBy">Identifier of the user creating the document.</param>
    /// <param name="docType">Document type key used by the ECM service.</param>
    /// <param name="status">Initial lifecycle status of the document.</param>
    /// <param name="sensitivity">Sensitivity level label associated with the document.</param>
    /// <param name="filePath">Absolute or relative path to the file being uploaded.</param>
    public DocumentUploadRequest(Guid ownerId, Guid createdBy, string docType, string status, string sensitivity, string filePath)
    {
        OwnerId = ownerId;
        CreatedBy = createdBy;
        DocType = docType;
        Status = status;
        Sensitivity = sensitivity;
        FilePath = filePath;
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
    public string DocType { get; init; }

    /// <summary>
    /// Initial workflow status applied to the document.
    /// </summary>
    public string Status { get; init; }

    /// <summary>
    /// Sensitivity label used to classify the document.
    /// </summary>
    public string Sensitivity { get; init; }

    /// <summary>
    /// Absolute or relative file system path to the binary payload.
    /// </summary>
    public string FilePath { get; init; }

    /// <summary>
    /// Optional identifier of the document type when the type key is insufficient.
    /// </summary>
    public Guid? DocumentTypeId { get; init; }

    /// <summary>
    /// Optional title that overrides the file name when present.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Optional MIME type sent to ECM when it cannot be inferred from the file.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Email of the acting user for on-behalf scenarios.
    /// </summary>
    public string? OnBehalfUserEmail { get; init; }
}
