using System;

namespace ECM.Document.Application.Documents.Commands;

public sealed record UpdateDocumentCommand(
    Guid DocumentId,
    Guid UpdatedBy,
    string? Title,
    string? Status,
    string? Sensitivity,
    bool HasGroupId,
    Guid? GroupId,
    bool HasDocumentTypeId,
    Guid? DocumentTypeId);
