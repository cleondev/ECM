using System;
using AppGateway.Infrastructure.Ecm;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace AppGateway.Infrastructure;

public static class DependencyInjection
{
    private const string HttpClientName = "ecm-api";

    public static IServiceCollection AddGatewayInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var baseAddress = configuration.GetValue<string>("Services:Ecm") ?? "http://localhost:8080";

        services.AddHttpClient(HttpClientName, client => client.BaseAddress = new Uri(baseAddress))
                .AddStandardResilienceHandler();

        services.AddScoped<IEcmApiClient>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            return new EcmApiClient(factory.CreateClient(HttpClientName));
        });

        return services;
    }
}
