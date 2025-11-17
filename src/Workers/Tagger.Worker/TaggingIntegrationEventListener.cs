using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Messaging;
using Workers.Shared.Messaging;
using Workers.Shared.Messaging.Kafka;

namespace Tagger;

internal sealed class TaggingIntegrationEventListener(
    IKafkaConsumer consumer,
    IServiceScopeFactory scopeFactory,
    ILogger<TaggingIntegrationEventListener> logger) : BackgroundService
{
    private const string DocumentUploadedTopic = EventTopics.Pipelines.Document.Uploaded;
    private const string OcrCompletedTopic = EventTopics.Pipelines.Ocr.Completed;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IKafkaConsumer _consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly ILogger<TaggingIntegrationEventListener> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var uploadTask = _consumer.ConsumeAsync(DocumentUploadedTopic, HandleDocumentUploadedAsync, stoppingToken);
        var ocrTask = _consumer.ConsumeAsync(OcrCompletedTopic, HandleOcrCompletedAsync, stoppingToken);
        return Task.WhenAll(uploadTask, ocrTask);
    }

    internal Task HandleDocumentUploadedAsync(KafkaMessage message, CancellationToken cancellationToken)
    {
        return KafkaIntegrationEventHandler.HandleMessageAsync<KafkaMessage, IntegrationEventEnvelope<DocumentIntegrationEventPayload>, TaggingEventProcessor>(
            message,
            static message => message.Topic,
            static message => message.Value,
            _scopeFactory,
            _logger,
            SerializerOptions,
            static envelope => envelope.EventId,
            static (envelope, processor, token) =>
            {
                var data = envelope.Data ?? throw new InvalidOperationException("Document uploaded event payload is missing data.");

                var integrationEvent = new DocumentUploadedIntegrationEvent(
                    envelope.EventId,
                    envelope.OccurredAtUtc,
                    data.DocumentId,
                    data.Title,
                    data.Summary,
                    data.Content,
                    DocumentIntegrationEventMetadataMapper.ToMetadataDictionary(data),
                    data.Tags);

                return processor.HandleDocumentUploadedAsync(integrationEvent, token);
            },
            cancellationToken);
    }

    internal Task HandleOcrCompletedAsync(KafkaMessage message, CancellationToken cancellationToken)
    {
        return KafkaIntegrationEventHandler.HandleMessageAsync<KafkaMessage, IntegrationEventEnvelope<DocumentIntegrationEventPayload>, TaggingEventProcessor>(
            message,
            static message => message.Topic,
            static message => message.Value,
            _scopeFactory,
            _logger,
            SerializerOptions,
            static envelope => envelope.EventId,
            static (envelope, processor, token) =>
            {
                var data = envelope.Data ?? throw new InvalidOperationException("OCR completed event payload is missing data.");

                var integrationEvent = new OcrCompletedIntegrationEvent(
                    envelope.EventId,
                    envelope.OccurredAtUtc,
                    data.DocumentId,
                    data.Title,
                    data.Summary,
                    data.Content,
                    DocumentIntegrationEventMetadataMapper.ToMetadataDictionary(data),
                    data.Tags);

                return processor.HandleOcrCompletedAsync(integrationEvent, token);
            },
            cancellationToken);
    }
}
