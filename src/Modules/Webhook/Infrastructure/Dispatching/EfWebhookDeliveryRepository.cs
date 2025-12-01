using ECM.Webhook.Application.Dispatching;
using ECM.Webhook.Domain;
using ECM.Webhook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECM.Webhook.Infrastructure.Dispatching;

internal sealed class EfWebhookDeliveryRepository(WebhookDbContext dbContext) : IWebhookDeliveryRepository
{
    public Task<WebhookDelivery?> FindAsync(string requestId, string endpointKey, CancellationToken cancellationToken = default)
    {
        return dbContext.WebhookDeliveries
            .AsNoTracking()
            .SingleOrDefaultAsync(
                delivery => delivery.RequestId == requestId && delivery.EndpointKey == endpointKey,
                cancellationToken);
    }

    public async Task AddAsync(WebhookDelivery delivery, CancellationToken cancellationToken = default)
    {
        await dbContext.WebhookDeliveries.AddAsync(delivery, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(WebhookDelivery delivery, CancellationToken cancellationToken = default)
    {
        dbContext.WebhookDeliveries.Update(delivery);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
