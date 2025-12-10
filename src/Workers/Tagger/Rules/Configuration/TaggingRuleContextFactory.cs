using System.Linq;

using Ecm.Rules.Abstractions;

using Tagger.Events;

namespace Tagger.Rules.Configuration;

internal interface ITaggingRuleContextFactory
{
    IRuleContext Create(ITaggingIntegrationEvent integrationEvent);
}

internal interface ITaggingRuleContextEnricher
{
    void Enrich(TaggingRuleContextBuilder builder, ITaggingIntegrationEvent integrationEvent);
}

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
