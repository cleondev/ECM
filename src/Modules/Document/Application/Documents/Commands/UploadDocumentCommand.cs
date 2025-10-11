using System;
using System.IO;

namespace ECM.Document.Application.Documents.Commands;

public sealed record UploadDocumentCommand(
    string Title,
    string DocType,
    string Status,
    Guid OwnerId,
    Guid CreatedBy,
    string? Department,
    string? Sensitivity,
    Guid? DocumentTypeId,
    string FileName,
    string ContentType,
    long FileSize,
    string Sha256,
    Stream Content);
