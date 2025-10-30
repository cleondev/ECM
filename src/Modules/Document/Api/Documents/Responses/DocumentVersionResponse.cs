namespace ECM.Document.Api.Documents.Responses;

public sealed record DocumentVersionResponse(
    Guid Id,
    int VersionNo,
    string StorageKey,
    long Bytes,
    string MimeType,
    string Sha256,
    Guid CreatedBy,
    DateTimeOffset CreatedAtUtc);
