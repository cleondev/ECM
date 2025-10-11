using ECM.BuildingBlocks.Domain.Events;
using ECM.File.Domain.Files.Events;

namespace ECM.File.Domain.Files;

public sealed class StoredFile : IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

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

    public static StoredFile Create(string storageKey, bool legalHold, DateTimeOffset createdAtUtc)
    {
        var storedFile = new StoredFile(storageKey, legalHold, createdAtUtc);
        storedFile.Raise(new StoredFileUploadedDomainEvent(storedFile.StorageKey, storedFile.LegalHold, storedFile.CreatedAtUtc));
        return storedFile;
    }

    public string StorageKey { get; }

    public bool LegalHold { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    private void Raise(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }
}
