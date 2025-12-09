using System;
using System.Collections.Generic;
using System.Linq;

using Ecm.Rules.Abstractions;

using Microsoft.Extensions.Options;

namespace Tagger.RulesConfiguration;

internal interface ITaggingRuleSetSelector
{
    IReadOnlyCollection<string> GetRuleSets(ITaggingIntegrationEvent integrationEvent);
}

internal sealed class TaggingRuleSetSelector : ITaggingRuleSetSelector
{
    private readonly IOptionsMonitor<TaggerRulesOptions> _options;

    public TaggingRuleSetSelector(IOptionsMonitor<TaggerRulesOptions> options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public IReadOnlyCollection<string> GetRuleSets(ITaggingIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var eventName = TaggingIntegrationEventNames.FromEvent(integrationEvent);
        var triggers = _options.CurrentValue.Triggers ?? Array.Empty<TaggerRuleTriggerOptions>();

        return triggers
            .Where(trigger => string.Equals(trigger.Event, eventName, StringComparison.OrdinalIgnoreCase))
            .SelectMany(trigger => trigger.RuleSets ?? Array.Empty<string>())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
