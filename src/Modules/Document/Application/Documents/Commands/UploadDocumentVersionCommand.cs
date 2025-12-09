using System;
using System.IO;

namespace ECM.Document.Application.Documents.Commands;

public sealed record UploadDocumentVersionCommand(
    Guid DocumentId,
    Guid? CreatedBy,
    string FileName,
    string ContentType,
    long FileSize,
    string Sha256,
    Stream Content);
