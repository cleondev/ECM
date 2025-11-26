using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Ecm.Sdk;

/// <summary>
/// Extension methods to register the ECM SDK with a dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers ECM SDK services using configuration from the provided root configuration.
    /// </summary>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="configuration">Configuration containing the <c>Ecm</c> section.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddEcmSdk(this IServiceCollection services, IConfiguration configuration) =>
        services.AddEcmSdk(configuration.GetSection("Ecm"));

    /// <summary>
    /// Registers ECM SDK services using configuration bound from the specified section.
    /// </summary>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="section">Configuration section containing ECM settings.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddEcmSdk(this IServiceCollection services, IConfigurationSection section)
    {
        services.AddEcmIntegrationOptions(section.Bind);
        return services.RegisterEcmIntegration();
    }

    /// <summary>
    /// Registers ECM SDK services using programmatic option configuration.
    /// </summary>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="configureOptions">Delegate used to configure <see cref="EcmIntegrationOptions"/>.</param>
    /// <returns>The configured service collection.</returns>
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
                options => !options.ApiKey.Enabled || !string.IsNullOrWhiteSpace(options.ApiKey.ApiKey),
                "Ecm:ApiKey:ApiKey must be configured when ApiKey.Enabled=true.")
            .Validate(
                options => !options.ApiKey.Enabled || !string.IsNullOrWhiteSpace(options.OnBehalfUserEmail) || options.OnBehalfUserId is not null,
                "Ecm:OnBehalfUserEmail or OnBehalfUserId must be configured when ApiKey.Enabled=true.")
            .Validate(
                options => !options.Sso.Enabled
                    || (!string.IsNullOrWhiteSpace(options.Sso.Authority)
                        && !string.IsNullOrWhiteSpace(options.Sso.ClientId)
                        && !string.IsNullOrWhiteSpace(options.Sso.ClientSecret)
                        && options.Sso.Scopes.Length > 0),
                "Ecm:Sso settings are incomplete when Sso:Enabled=true.")
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
