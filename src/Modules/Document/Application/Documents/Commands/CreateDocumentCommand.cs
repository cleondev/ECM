using System;

namespace ECM.Document.Application.Documents.Commands;

public sealed record CreateDocumentCommand(
    string Title,
    string DocType,
    string Status,
    Guid OwnerId,
    Guid CreatedBy,
    Guid? GroupId,
    string? Sensitivity,
    Guid? DocumentTypeId);
