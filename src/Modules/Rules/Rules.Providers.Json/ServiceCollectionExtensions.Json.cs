using System.Text.Json;
using Ecm.Rules.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Ecm.Rules.Providers.Json;

public static class JsonRuleServiceCollectionExtensions
{
    public static IServiceCollection AddJsonRuleProviderFromFile(
        this IServiceCollection services,
        string filePath)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        services.AddSingleton<IRuleProvider>(_ =>
        {
            var json = File.ReadAllText(filePath);
            var defs = JsonSerializer.Deserialize<List<JsonRuleSetDefinition>>(json)
                       ?? new List<JsonRuleSetDefinition>();

            return new JsonRuleProvider(defs);
        });

        return services;
    }
}
