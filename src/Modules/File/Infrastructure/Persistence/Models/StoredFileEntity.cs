using System;

namespace ECM.File.Infrastructure.Persistence.Models;

public sealed class StoredFileEntity
{
    public string StorageKey { get; set; } = string.Empty;

    public bool LegalHold { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
