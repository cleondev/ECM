using System.Text.Json;
using ECM.Webhook.Application.Dispatching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Webhooks;
using Workers.Shared.Messaging;
using Workers.Shared.Messaging.Kafka;

namespace WebhookDispatcher;

internal sealed class WebhookRequestListener(
    IKafkaConsumer consumer,
    IServiceScopeFactory scopeFactory,
    ILogger<WebhookRequestListener> logger) : BackgroundService
{
    private const string Topic = "webhook-requests";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IKafkaConsumer _consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly ILogger<WebhookRequestListener> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _consumer.ConsumeAsync(Topic, HandleWebhookRequestedAsync, stoppingToken);
    }

    internal async Task HandleWebhookRequestedAsync(KafkaMessage message, CancellationToken cancellationToken)
    {
        WebhookRequested? request;

        try
        {
            request = JsonSerializer.Deserialize<WebhookRequested>(message.Value, SerializerOptions);
        }
        catch (JsonException exception)
        {
            _logger.LogError(exception, "Failed to deserialize webhook request payload from topic {Topic}.", message.Topic);
            return;
        }

        if (request is null)
        {
            _logger.LogWarning("Received webhook request with empty payload from topic {Topic}.", message.Topic);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<WebhookDispatchService>();
        await dispatcher.DispatchAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
