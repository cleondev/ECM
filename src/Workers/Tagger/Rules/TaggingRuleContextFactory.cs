using System;
using System.Collections.Generic;
using System.Linq;

namespace Tagger;

internal interface ITaggingRuleContextFactory
{
    TaggingRuleContext Create(ITaggingIntegrationEvent integrationEvent);
}

internal interface ITaggingRuleContextEnricher
{
    void Enrich(TaggingRuleContextBuilder builder, ITaggingIntegrationEvent integrationEvent);
}

internal sealed class TaggingRuleContextFactory : ITaggingRuleContextFactory
{
    private readonly IReadOnlyCollection<ITaggingRuleContextEnricher> _enrichers;

    public TaggingRuleContextFactory(IEnumerable<ITaggingRuleContextEnricher> enrichers)
    {
        _enrichers = enrichers?.ToArray() ?? throw new ArgumentNullException(nameof(enrichers));
    }

    public TaggingRuleContext Create(ITaggingIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var builder = TaggingRuleContextBuilder.FromIntegrationEvent(integrationEvent);

        foreach (var enricher in _enrichers)
        {
            enricher.Enrich(builder, integrationEvent);
        }

        return builder.Build();
    }
}
