using System;

namespace ECM.Document.Application.Documents.Commands;

public sealed record UpdateDocumentCommand(
    Guid DocumentId,
    Guid UpdatedBy,
    string? Title,
    string? Status,
    string? Sensitivity,
    Guid? GroupId,
    Guid? DocumentTypeId);
