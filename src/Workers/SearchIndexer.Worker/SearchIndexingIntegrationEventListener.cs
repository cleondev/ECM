using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ECM.SearchIndexer.Application.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SearchIndexer.Messaging;
using Shared.Contracts.Messaging;

namespace SearchIndexer;

internal sealed class SearchIndexingIntegrationEventListener(
    IKafkaConsumer consumer,
    IServiceScopeFactory scopeFactory,
    ILogger<SearchIndexingIntegrationEventListener> logger) : BackgroundService
{
    private const string DocumentUploadedTopic = EventTopics.Document.Uploaded;
    private const string OcrCompletedTopic = EventTopics.Ocr.Completed;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IKafkaConsumer _consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly ILogger<SearchIndexingIntegrationEventListener> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var documentTask = _consumer.ConsumeAsync(DocumentUploadedTopic, HandleDocumentUploadedAsync, stoppingToken);
        var ocrTask = _consumer.ConsumeAsync(OcrCompletedTopic, HandleOcrCompletedAsync, stoppingToken);

        return Task.WhenAll(documentTask, ocrTask);
    }

    internal Task HandleDocumentUploadedAsync(KafkaMessage message, CancellationToken cancellationToken)
    {
        return HandleMessageAsync(message, static (envelope, processor, token) =>
        {
            var data = envelope.Data ?? throw new InvalidOperationException("Document uploaded event payload is missing data.");

            var integrationEvent = new DocumentUploadedIntegrationEvent(
                envelope.EventId,
                envelope.OccurredAtUtc,
                data.DocumentId,
                data.Title,
                data.Summary,
                data.Content,
                data.Metadata,
                data.Tags);

            return processor.HandleDocumentUploadedAsync(integrationEvent, token);
        }, cancellationToken);
    }

    internal Task HandleOcrCompletedAsync(KafkaMessage message, CancellationToken cancellationToken)
    {
        return HandleMessageAsync(message, static (envelope, processor, token) =>
        {
            var data = envelope.Data ?? throw new InvalidOperationException("OCR completed event payload is missing data.");

            var integrationEvent = new OcrCompletedIntegrationEvent(
                envelope.EventId,
                envelope.OccurredAtUtc,
                data.DocumentId,
                data.Title,
                data.Summary,
                data.Content,
                data.Metadata,
                data.Tags);

            return processor.HandleOcrCompletedAsync(integrationEvent, token);
        }, cancellationToken);
    }

    private async Task HandleMessageAsync(
        KafkaMessage message,
        Func<SearchIndexingIntegrationEventEnvelope, SearchIndexingEventProcessor, CancellationToken, Task> handler,
        CancellationToken cancellationToken)
    {
        SearchIndexingIntegrationEventEnvelope? envelope;

        try
        {
            envelope = JsonSerializer.Deserialize<SearchIndexingIntegrationEventEnvelope>(message.Value, SerializerOptions);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to deserialize message from topic {Topic} with payload {Payload}.",
                message.Topic,
                message.Value);
            return;
        }

        if (envelope is null)
        {
            _logger.LogWarning(
                "Received null payload from topic {Topic}. Ignoring message.",
                message.Topic);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<SearchIndexingEventProcessor>();

        try
        {
            await handler(envelope, processor, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                exception,
                "Failed to process message {EventId} from topic {Topic}.",
                envelope.EventId,
                message.Topic);
        }
    }

    private sealed record SearchIndexingIntegrationEventEnvelope(
        Guid EventId,
        DateTimeOffset OccurredAtUtc,
        SearchIndexingIntegrationEventPayload? Data);

    private sealed record SearchIndexingIntegrationEventPayload(
        Guid DocumentId,
        string Title,
        string? Summary,
        string? Content,
        IDictionary<string, string>? Metadata,
        IReadOnlyCollection<string>? Tags);
}
