using Ecm.Sdk.Authentication;
using Ecm.Sdk.Clients;
using Ecm.Sdk.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Ecm.Sdk.Extensions;

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
                options => !string.IsNullOrWhiteSpace(options.ApiKey.ApiKey),
                "Ecm:ApiKey:ApiKey must be configured.")
            .ValidateOnStart();
    }

    private static IServiceCollection RegisterEcmIntegration(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddHttpContextAccessor();
        services.AddScoped<EcmAuthenticator>();
        services.AddTransient<EcmAccessTokenHandler>();

        services.AddHttpClient<EcmAuthenticator>();

        services.AddHttpClient<EcmFileClient>()
            .AddHttpMessageHandler<EcmAccessTokenHandler>();

        return services;
    }
}
