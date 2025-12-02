using System.Text.Json;

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
    JsonElement? Metadata,
    IReadOnlyCollection<string>? Tags,
    IReadOnlyCollection<Guid>? GroupIds);
