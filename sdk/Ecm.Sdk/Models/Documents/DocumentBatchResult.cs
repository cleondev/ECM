namespace Ecm.Sdk.Models.Documents;

/// <summary>
/// Describes a failed upload entry when performing a batch upload.
/// </summary>
/// <param name="FileName">Name of the file that failed.</param>
/// <param name="Message">Reason describing the failure.</param>
public sealed record DocumentUploadFailure(string FileName, string Message);

/// <summary>
/// Represents the outcome of a batch upload request.
/// </summary>
/// <param name="Documents">Documents that were uploaded successfully.</param>
/// <param name="Failures">Any files that failed to upload with their error details.</param>
public sealed record DocumentBatchResult(
    IReadOnlyCollection<DocumentDto> Documents,
    IReadOnlyCollection<DocumentUploadFailure> Failures);
