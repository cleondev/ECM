using System;
using System.Collections.Generic;

namespace Workers.Shared.Messaging;

public sealed record IntegrationEventEnvelope<TPayload>(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    TPayload? Data)
    where TPayload : class;

public sealed record DocumentIntegrationEventPayload(
    Guid DocumentId,
    string Title,
    string? Summary,
    string? Content,
    IDictionary<string, string>? Metadata,
    IReadOnlyCollection<string>? Tags);
