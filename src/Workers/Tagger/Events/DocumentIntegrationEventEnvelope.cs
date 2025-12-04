using System;

using Workers.Shared.Messaging;

namespace Tagger;

internal sealed record DocumentIntegrationEventEnvelope(
    Guid EventId,
    string Type,
    DateTimeOffset OccurredAtUtc,
    DocumentIntegrationEventPayload? Data);
