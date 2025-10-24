using System.Collections.Generic;

namespace ECM.Ocr.Application.Events;

public sealed record DocumentUploadedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid DocumentId,
    string Title,
    string? Summary,
    string? Content,
    IDictionary<string, string>? Metadata,
    IReadOnlyCollection<string>? Tags);
