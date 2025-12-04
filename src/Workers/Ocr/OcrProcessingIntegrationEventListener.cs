using System.Text.Json;

using ECM.Ocr.Application.Events;

using Shared.Contracts.Messaging;

using Workers.Shared.Messaging;
using Workers.Shared.Messaging.Kafka;

namespace Ocr;

internal sealed class OcrProcessingIntegrationEventListener(
    IKafkaConsumer consumer,
    IServiceScopeFactory scopeFactory,
    ILogger<OcrProcessingIntegrationEventListener> logger) : BackgroundService
{
    private const string DocumentUploadedTopic = EventNames.Document.Uploaded;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IKafkaConsumer _consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly ILogger<OcrProcessingIntegrationEventListener> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _consumer.ConsumeAsync(DocumentUploadedTopic, HandleDocumentUploadedAsync, stoppingToken);
    }

    internal Task HandleDocumentUploadedAsync(KafkaMessage message, CancellationToken cancellationToken)
    {
        return KafkaIntegrationEventHandler.HandleMessageAsync<KafkaMessage, IntegrationEventEnvelope<DocumentIntegrationEventPayload>, OcrProcessingEventProcessor>(
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
}
