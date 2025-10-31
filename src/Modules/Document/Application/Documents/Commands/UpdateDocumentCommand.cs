using System;

namespace ECM.Document.Application.Documents.Commands;

public sealed record UpdateDocumentCommand(
    Guid DocumentId,
    string? Title,
    string? Status,
    string? Sensitivity,
    bool HasGroupId,
    Guid? GroupId);
