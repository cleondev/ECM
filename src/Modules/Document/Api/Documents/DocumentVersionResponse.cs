namespace ECM.Document.Api.Documents;

public sealed record DocumentVersionResponse(
    Guid Id,
    int VersionNo,
    string StorageKey,
    long Size,
    string MimeType,
    string Sha256,
    Guid CreatedBy,
    DateTimeOffset CreatedAtUtc);
