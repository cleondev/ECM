using System;
using System.Collections.Generic;

namespace Workers.Shared.Messaging;

internal sealed record IntegrationEventEnvelope<TPayload>(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    TPayload? Data)
    where TPayload : class;

internal sealed record DocumentIntegrationEventPayload(
    Guid DocumentId,
    string Title,
    string? Summary,
    string? Content,
    IDictionary<string, string>? Metadata,
    IReadOnlyCollection<string>? Tags);
