using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Workers.Shared.Messaging;

public static class KafkaIntegrationEventHandler
{
    public static async Task HandleMessageAsync<TMessage, TEnvelope, TProcessor>(
        TMessage message,
        Func<TMessage, string> topicSelector,
        Func<TMessage, string> payloadSelector,
        IServiceScopeFactory scopeFactory,
        ILogger logger,
        JsonSerializerOptions serializerOptions,
        Func<TEnvelope, Guid> eventIdSelector,
        Func<TEnvelope, TProcessor, CancellationToken, Task> handler,
        CancellationToken cancellationToken)
        where TEnvelope : class
    {
        var topic = topicSelector(message);
        var payload = payloadSelector(message);

        TEnvelope? envelope;

        try
        {
            envelope = JsonSerializer.Deserialize<TEnvelope>(payload, serializerOptions);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(
                exception,
                "Failed to deserialize message from topic {Topic} with payload {Payload}.",
                topic,
                payload);
            return;
        }

        if (envelope is null)
        {
            logger.LogWarning(
                "Received null payload from topic {Topic}. Ignoring message.",
                topic);
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<TProcessor>();

        try
        {
            await handler(envelope, processor, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            var eventId = eventIdSelector(envelope);

            logger.LogError(
                exception,
                "Failed to process message {EventId} from topic {Topic}.",
                eventId,
                topic);
        }
    }
}
