using System.Linq;

using Ecm.Rules.Abstractions;

using Tagger.Events;

namespace Tagger.Rules.Configuration;

internal interface ITaggingRuleContextFactory
{
    /// <summary>
    /// Builds a rule context from a tagging integration event so rules can access event fields uniformly.
    /// </summary>
    IRuleContext Create(ITaggingIntegrationEvent integrationEvent);
}

internal interface ITaggingRuleContextEnricher
{
    /// <summary>
    /// Enriches the rule context builder with additional derived values from a tagging event.
    /// </summary>
    void Enrich(TaggingRuleContextBuilder builder, ITaggingIntegrationEvent integrationEvent);
}

/// <summary>
/// Combines raw integration event data with optional enrichers to produce a rule context for evaluation.
/// </summary>
internal sealed class TaggingRuleContextFactory : ITaggingRuleContextFactory
{
    private readonly IReadOnlyCollection<ITaggingRuleContextEnricher> _enrichers;
    private readonly IRuleContextFactory _ruleContextFactory;

    public TaggingRuleContextFactory(
        IRuleContextFactory ruleContextFactory,
        IEnumerable<ITaggingRuleContextEnricher> enrichers)
    {
        _ruleContextFactory = ruleContextFactory ?? throw new ArgumentNullException(nameof(ruleContextFactory));
        _enrichers = enrichers?.ToArray() ?? throw new ArgumentNullException(nameof(enrichers));
    }

    public IRuleContext Create(ITaggingIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var builder = TaggingRuleContextBuilder.FromIntegrationEvent(integrationEvent);

        foreach (var enricher in _enrichers)
        {
            enricher.Enrich(builder, integrationEvent);
        }

        var items = builder.Build();
        var dictionary = new Dictionary<string, object>(items, StringComparer.OrdinalIgnoreCase);

        return _ruleContextFactory.FromDictionary(dictionary);
    }
}
