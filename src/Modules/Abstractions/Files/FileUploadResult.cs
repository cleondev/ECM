using System;

namespace ECM.Abstractions.Files;

public sealed record FileUploadResult(
    string StorageKey,
    string FileName,
    string ContentType,
    long Length,
    DateTimeOffset CreatedAtUtc);
