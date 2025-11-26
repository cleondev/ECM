using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Ecm.Sdk;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEcmSdk(this IServiceCollection services, IConfiguration configuration) =>
        services.AddEcmSdk(configuration.GetSection("Ecm"));

    public static IServiceCollection AddEcmSdk(this IServiceCollection services, IConfigurationSection section)
    {
        services.AddEcmIntegrationOptions(section.Bind);
        return services.RegisterEcmIntegration();
    }

    public static IServiceCollection AddEcmSdk(
        this IServiceCollection services,
        Action<EcmIntegrationOptions> configureOptions)
    {
        services.AddEcmIntegrationOptions(configureOptions);
        return services.RegisterEcmIntegration();
    }

    private static OptionsBuilder<EcmIntegrationOptions> AddEcmIntegrationOptions(
        this IServiceCollection services,
        Action<EcmIntegrationOptions> configureOptions)
    {
        return services
            .AddOptions<EcmIntegrationOptions>()
            .Configure(configureOptions)
            .Validate(options => !string.IsNullOrWhiteSpace(options.BaseUrl), "Ecm:BaseUrl must be configured.")
            .Validate(
                options => !options.OnBehalf.Enabled || !string.IsNullOrWhiteSpace(options.OnBehalf.ApiKey),
                "Ecm:OnBehalf:ApiKey must be configured when OnBehalf.Enabled=true.")
            .Validate(
                options => !options.OnBehalf.Enabled || !string.IsNullOrWhiteSpace(options.OnBehalf.UserEmail) || options.OnBehalf.UserId is not null,
                "Ecm:OnBehalf:UserEmail or UserId must be configured when OnBehalf.Enabled=true.")
            .Validate(
                options => !options.OnBehalf.Sso.Enabled || options.OnBehalf.Enabled,
                "Ecm:OnBehalf:Sso.Enabled requires OnBehalf.Enabled=true.")
            .Validate(
                options => !options.OnBehalf.Sso.Enabled
                    || (!string.IsNullOrWhiteSpace(options.OnBehalf.Sso.Authority)
                        && !string.IsNullOrWhiteSpace(options.OnBehalf.Sso.ClientId)
                        && !string.IsNullOrWhiteSpace(options.OnBehalf.Sso.ClientSecret)
                        && options.OnBehalf.Sso.Scopes.Length > 0),
                "Ecm:OnBehalf:Sso settings are incomplete when OnBehalf:Sso:Enabled=true.")
            .ValidateOnStart();
    }

    private static IServiceCollection RegisterEcmIntegration(this IServiceCollection services)
    {
        services.AddSingleton(new CookieContainer());
        services.AddScoped<EcmAccessTokenProvider>();
        services.AddScoped<EcmSsoTokenProvider>();
        services.AddScoped<EcmOnBehalfAuthenticator>();
        services.AddTransient<EcmAccessTokenHandler>();

        services.AddHttpClient<EcmFileClient>((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<EcmIntegrationOptions>>().Value;

                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(100);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("ecm-sdk/1.0");
            })
            .AddHttpMessageHandler<EcmAccessTokenHandler>()
            .ConfigurePrimaryHttpMessageHandler(serviceProvider => new HttpClientHandler
            {
                CookieContainer = serviceProvider.GetRequiredService<CookieContainer>(),
                UseCookies = true,
                AutomaticDecompression = DecompressionMethods.All,
            });

        return services;
    }
}
