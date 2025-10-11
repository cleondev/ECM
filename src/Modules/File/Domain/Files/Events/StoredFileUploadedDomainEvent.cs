using ECM.BuildingBlocks.Domain.Events;

namespace ECM.File.Domain.Files.Events;

public sealed record StoredFileUploadedDomainEvent(
    string StorageKey,
    bool LegalHold,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
