using Ecm.Rules.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Ecm.Rules.Engine;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRuleEngine(
        this IServiceCollection services,
        Action<RuleEngineOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new RuleEngineOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<IRuleContextFactory, RuleContextFactory>();
        services.AddSingleton<IRuleEngine, RuleEngine>();

        return services;
    }
}
