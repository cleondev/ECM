using Ecm.Rules.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Tagger.Rules.Enrichers;

namespace Tagger.Rules.Configuration;

internal static class RulesServiceCollectionExtensions
{
    public static IServiceCollection AddTaggerRules(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<IValidateOptions<TaggerRulesOptions>, TaggerRulesOptionsValidator>();
        services.AddSingleton<IConfigureOptions<TaggerRulesOptions>, TaggerRulesOptionsSetup>();
        services.AddSingleton<IConfigureOptions<TaggerRulesOptions>, TaggerRuleTriggersSetup>();

        services
            .AddOptions<TaggerRulesOptions>()
            .Bind(configuration.GetSection(TaggerRulesOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IRuleProvider, TaggerRuleProvider>();
        services.AddSingleton<IRuleProvider, BuiltInRuleProvider>();

        services.AddSingleton<ITaggingRuleSetSelector, TaggingRuleSetSelector>();

        services.AddSingleton<ITaggingRuleContextEnricher, DocumentTypeContextEnricher>();

        return services;
    }
}
