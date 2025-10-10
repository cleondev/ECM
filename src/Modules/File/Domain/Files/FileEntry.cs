using System;

namespace ECM.File.Domain.Files;

public sealed class FileEntry
{
    public FileEntry(FileId id, FileMetadata metadata, string storageKey, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Metadata = metadata;
        StorageKey = storageKey;
        CreatedAtUtc = createdAtUtc;
    }

    public FileId Id { get; }

    public FileMetadata Metadata { get; }

    public string StorageKey { get; }

    public DateTimeOffset CreatedAtUtc { get; }
}
