using System.Collections.Concurrent;
using ECM.Webhook.Application.Dispatching;
using ECM.Webhook.Domain;

namespace ECM.Webhook.Infrastructure.Dispatching;

internal sealed class InMemoryWebhookDeliveryRepository : IWebhookDeliveryRepository
{
    private readonly ConcurrentDictionary<string, WebhookDelivery> _deliveries = new(StringComparer.OrdinalIgnoreCase);

    public Task<WebhookDelivery?> FindAsync(string requestId, string endpointKey, CancellationToken cancellationToken = default)
    {
        var key = BuildKey(requestId, endpointKey);
        _deliveries.TryGetValue(key, out var delivery);
        return Task.FromResult(delivery);
    }

    public Task AddAsync(WebhookDelivery delivery, CancellationToken cancellationToken = default)
    {
        var key = BuildKey(delivery.RequestId, delivery.EndpointKey);
        _deliveries[key] = delivery;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(WebhookDelivery delivery, CancellationToken cancellationToken = default)
    {
        var key = BuildKey(delivery.RequestId, delivery.EndpointKey);
        _deliveries[key] = delivery;
        return Task.CompletedTask;
    }

    private static string BuildKey(string requestId, string endpointKey)
    {
        return $"{requestId}::{endpointKey}";
    }
}
