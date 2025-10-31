using System;

namespace ECM.Document.Application.Documents.Commands;

public sealed record DeleteDocumentCommand(Guid DocumentId);
