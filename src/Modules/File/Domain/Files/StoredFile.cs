using System;

namespace ECM.File.Domain.Files;

public sealed class StoredFile
{
    public StoredFile(string storageKey, bool legalHold, DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException("Storage key is required.", nameof(storageKey));
        }

        StorageKey = storageKey.Trim();
        LegalHold = legalHold;
        CreatedAtUtc = createdAtUtc;
    }

    public string StorageKey { get; }

    public bool LegalHold { get; }

    public DateTimeOffset CreatedAtUtc { get; }
}
