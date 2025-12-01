using ECM.Webhook.Domain;

namespace ECM.Webhook.Application.Dispatching;

public interface IWebhookDeliveryRepository
{
    Task<WebhookDelivery?> FindAsync(string requestId, string endpointKey, CancellationToken cancellationToken = default);

    Task AddAsync(WebhookDelivery delivery, CancellationToken cancellationToken = default);

    Task UpdateAsync(WebhookDelivery delivery, CancellationToken cancellationToken = default);
}
