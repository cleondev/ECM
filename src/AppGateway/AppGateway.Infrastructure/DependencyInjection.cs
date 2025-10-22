using System;
using AppGateway.Infrastructure.IAM;
using AppGateway.Infrastructure.Ecm;

using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;

namespace AppGateway.Infrastructure;

public static class DependencyInjection
{
    private const string HttpClientName = "ecm-api";

    public static IServiceCollection AddGatewayInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var baseAddress = configuration.GetValue<string>("Services:Ecm") ?? "http://localhost:8080";
        var scope = configuration.GetValue<string>("Services:EcmScope") ?? "api://istsvn.onmicrosoft.com/ecm-host/.default";
        var tenantId = configuration.GetValue<string>("Services:EcmTenantId") ?? configuration.GetValue<string>("AzureAd:TenantId");
        var authenticationScheme = configuration.GetValue<string>("Services:EcmAuthenticationScheme");

        services.AddHttpClient(HttpClientName, client => client.BaseAddress = new Uri(baseAddress))
                .AddStandardResilienceHandler();

        services.Configure<IamOptions>(configuration.GetSection("IAM"));
        services.Configure<EcmApiClientOptions>(options =>
        {
            options.Scope = scope;
            options.TenantId = tenantId;
            if (!string.IsNullOrWhiteSpace(authenticationScheme))
            {
                options.AuthenticationScheme = authenticationScheme!;
            }
            else
            {
                options.AuthenticationScheme = OpenIdConnectDefaults.AuthenticationScheme;
            }
        });

        services.AddHttpContextAccessor();

        services.AddScoped<IEcmApiClient>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var accessor = sp.GetRequiredService<IHttpContextAccessor>();
            var tokenAcquisition = sp.GetRequiredService<ITokenAcquisition>();
            var options = sp.GetRequiredService<IOptions<EcmApiClientOptions>>();
            var logger = sp.GetRequiredService<ILogger<EcmApiClient>>();

            return new EcmApiClient(
                factory.CreateClient(HttpClientName),
                accessor,
                tokenAcquisition,
                options,
                logger);
        });

        return services;
    }
}
