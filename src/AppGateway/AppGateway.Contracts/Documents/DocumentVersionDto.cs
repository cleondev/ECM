namespace AppGateway.Contracts.Documents;

public sealed record DocumentVersionDto(
    Guid Id,
    int VersionNo,
    string StorageKey,
    long Bytes,
    string MimeType,
    string Sha256,
    Guid CreatedBy,
    DateTimeOffset CreatedAtUtc);
