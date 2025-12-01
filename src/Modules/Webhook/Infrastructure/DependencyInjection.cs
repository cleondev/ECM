using ECM.BuildingBlocks.Infrastructure.Configuration;
using ECM.Webhook.Application.Dispatching;
using ECM.Webhook.Infrastructure.Dispatching;
using ECM.Webhook.Infrastructure.Persistence;
using EFCore.NamingConventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.Webhook.Infrastructure;

public static class WebhookInfrastructureModuleExtensions
{
    public static IServiceCollection AddWebhookInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<WebhookDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetRequiredConnectionStringForModule("Webhook");

            options.UseNpgsql(
                    connectionString,
                    builder => builder.MigrationsAssembly(typeof(WebhookDbContext).Assembly.FullName))
                .UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IWebhookDeliveryRepository, EfWebhookDeliveryRepository>();
        return services;
    }
}
