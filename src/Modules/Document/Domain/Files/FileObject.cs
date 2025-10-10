namespace ECM.Document.Domain.Files;

public sealed class FileObject
{
    private FileObject()
    {
        StorageKey = null!;
    }

    public FileObject(string storageKey, bool legalHold, DateTimeOffset createdAtUtc)
        : this()
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException("Storage key is required.", nameof(storageKey));
        }

        StorageKey = storageKey.Trim();
        LegalHold = legalHold;
        CreatedAtUtc = createdAtUtc;
    }

    public string StorageKey { get; private set; }

    public bool LegalHold { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
}
