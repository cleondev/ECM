namespace Shared.Contracts.Files;

public sealed record StoredFileUploadedContract(
    string StorageKey,
    bool LegalHold,
    DateTimeOffset UploadedAtUtc);
