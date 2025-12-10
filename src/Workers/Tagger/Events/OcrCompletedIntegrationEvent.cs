using System;
using System.Collections.Generic;

namespace Tagger.Events;

internal sealed record OcrCompletedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid DocumentId,
    string Title,
    string? Summary,
    string? Content,
    IDictionary<string, string>? Metadata,
    IReadOnlyCollection<string>? Tags) : ITaggingIntegrationEvent;
