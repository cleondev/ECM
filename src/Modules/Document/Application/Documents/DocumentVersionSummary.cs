namespace ECM.Document.Application.Documents;

public sealed record DocumentVersionSummary(
    Guid Id,
    int VersionNo,
    string StorageKey,
    long Bytes,
    string MimeType,
    string Sha256,
    Guid CreatedBy,
    DateTimeOffset CreatedAtUtc);
