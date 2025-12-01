using ECM.Webhook.Application.Dispatching;
using ECM.Webhook.Infrastructure.Dispatching;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.Webhook.Infrastructure;

public static class WebhookInfrastructureModuleExtensions
{
    public static IServiceCollection AddWebhookInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IWebhookDeliveryRepository, InMemoryWebhookDeliveryRepository>();
        return services;
    }
}
