namespace ECM.Document.Application.Documents;

public sealed record CreateDocumentCommand(
    string Title,
    string DocType,
    string Status,
    Guid OwnerId,
    Guid CreatedBy,
    string? Department,
    string? Sensitivity,
    Guid? DocumentTypeId);
