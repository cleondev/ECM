using ECM.Webhook.Application.Dispatching;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.Webhook.Application;

public static class WebhookApplicationModuleExtensions
{
    public static IServiceCollection AddWebhookApplication(this IServiceCollection services)
    {
        services.AddScoped<WebhookDispatchService>();
        return services;
    }
}
