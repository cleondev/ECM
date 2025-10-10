namespace ECM.Document.Api.Documents;

public sealed record CreateDocumentRequest(
    string Title,
    string DocType,
    string Status,
    Guid OwnerId,
    Guid CreatedBy,
    string? Department,
    string? Sensitivity,
    Guid? DocumentTypeId);
