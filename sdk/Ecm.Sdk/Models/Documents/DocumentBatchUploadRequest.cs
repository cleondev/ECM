namespace Ecm.Sdk.Models.Documents;

/// <summary>
/// Describes a single file that should be uploaded as part of a batch request.
/// </summary>
/// <param name="FilePath">Absolute or relative path to the file on disk.</param>
/// <param name="FileName">Optional original file name preserved for downstream consumers.</param>
/// <param name="ContentType">Optional MIME type sent to ECM when it cannot be inferred.</param>
public sealed record DocumentUploadFile(string FilePath, string? FileName = null, string? ContentType = null);

/// <summary>
/// Represents the information required to upload multiple documents in a single request.
/// </summary>
public sealed class DocumentBatchUploadRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentBatchUploadRequest"/> class.
    /// </summary>
    /// <param name="ownerId">Identifier of the owning user or group.</param>
    /// <param name="createdBy">Identifier of the user creating the document.</param>
    /// <param name="docType">Document type key used by the ECM service.</param>
    /// <param name="status">Initial lifecycle status of the document.</param>
    /// <param name="sensitivity">Sensitivity level label associated with the document.</param>
    /// <param name="files">Collection of files to upload.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="files"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="files"/> is empty.</exception>
    public DocumentBatchUploadRequest(
        Guid ownerId,
        Guid createdBy,
        string docType,
        string status,
        string sensitivity,
        IReadOnlyCollection<DocumentUploadFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        if (files.Count == 0)
        {
            throw new ArgumentException("At least one file is required for batch upload.", nameof(files));
        }

        OwnerId = ownerId;
        CreatedBy = createdBy;
        DocType = docType;
        Status = status;
        Sensitivity = sensitivity;
        Files = files;
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
    /// Optional identifier of the document type when the type key is insufficient.
    /// </summary>
    public Guid? DocumentTypeId { get; init; }

    /// <summary>
    /// Optional title that is applied to each uploaded file. ECM will append an index when multiple files are provided.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Optional workflow definition key to trigger after upload.
    /// </summary>
    public string? FlowDefinition { get; init; }

    /// <summary>
    /// Optional tags applied to each uploaded document.
    /// </summary>
    public IReadOnlyCollection<Guid> TagIds { get; init; } = Array.Empty<Guid>();

    /// <summary>
    /// Files that will be uploaded in the batch request.
    /// </summary>
    public IReadOnlyCollection<DocumentUploadFile> Files { get; init; }
}
