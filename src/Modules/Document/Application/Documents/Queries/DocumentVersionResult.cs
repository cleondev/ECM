using System;

namespace ECM.Document.Application.Documents.Queries;

public sealed record DocumentVersionResult(
    Guid Id,
    Guid DocumentId,
    string StorageKey,
    long Bytes,
    string MimeType,
    Guid CreatedBy,
    DateTimeOffset CreatedAtUtc);
