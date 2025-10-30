using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Messaging;
using Workers.Shared.Messaging;

namespace OutboxDispatcher;

/// <summary>
///     Responsible for translating outbox rows to integration events and publishing them to Kafka.
/// </summary>
internal sealed class OutboxMessageDispatcher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private static readonly IReadOnlyDictionary<string, string> AggregateTopics =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["document"] = EventTopics.Document.Events,
            ["tag-label"] = EventTopics.Document.Events,
            ["file"] = EventTopics.Document.Events,
            ["user"] = EventTopics.Iam.Events,
            ["access-relation"] = EventTopics.Iam.Events
        };

    private readonly IKafkaProducer _producer;
    private readonly ILogger<OutboxMessageDispatcher> _logger;

    public OutboxMessageDispatcher(IKafkaProducer producer, ILogger<OutboxMessageDispatcher> logger)
    {
        _producer = producer ?? throw new ArgumentNullException(nameof(producer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task DispatchAsync(PendingOutboxMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        if (!AggregateTopics.TryGetValue(message.Aggregate, out var topic))
        {
            throw new InvalidOperationException(
                $"No Kafka topic mapping configured for aggregate '{message.Aggregate}'.");
        }

        using var document = JsonDocument.Parse(message.Payload);
        var payload = document.RootElement.Clone();

        var envelope = new OutboxIntegrationEvent(
            EventId: Guid.CreateVersion7(message.OccurredAtUtc),
            Type: message.Type,
            Aggregate: message.Aggregate,
            AggregateId: message.AggregateId,
            OccurredAtUtc: message.OccurredAtUtc,
            Data: payload);

        var serialized = JsonSerializer.Serialize(envelope, SerializerOptions);
        var key = message.AggregateId.ToString();

        await _producer.ProduceAsync(topic, key, serialized, cancellationToken).ConfigureAwait(false);

        _logger.LogDebug(
            "Published outbox message {MessageId} of type {MessageType} to topic {Topic}.",
            message.Id,
            message.Type,
            topic);
    }

    private sealed record OutboxIntegrationEvent(
        Guid EventId,
        string Type,
        string Aggregate,
        Guid AggregateId,
        DateTimeOffset OccurredAtUtc,
        JsonElement Data);
}
