using System;
using System.Collections.Generic;

namespace ECM.Document.Application.Documents.Commands;

public sealed record CreateDocumentCommand(
    string Title,
    string DocType,
    string Status,
    Guid OwnerId,
    Guid CreatedBy,
    Guid? GroupId,
    IReadOnlyCollection<Guid> GroupIds,
    string? Sensitivity,
    Guid? DocumentTypeId);
