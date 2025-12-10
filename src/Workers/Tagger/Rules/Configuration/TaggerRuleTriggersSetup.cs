using System;
using System.Linq;
using Microsoft.Extensions.Options;

using Tagger.Events;
using Tagger.Rules.Configuration;
namespace Tagger.Rules.Configuration;

internal sealed class TaggerRuleTriggersSetup : IConfigureOptions<TaggerRulesOptions>
{
    public void Configure(TaggerRulesOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        AddTriggerIfMissing(options, TaggingIntegrationEventNames.DocumentUploaded, TaggingRuleSetNames.DocumentUploaded);
        AddTriggerIfMissing(options, TaggingIntegrationEventNames.OcrCompleted, TaggingRuleSetNames.OcrCompleted);
    }

    private static void AddTriggerIfMissing(TaggerRulesOptions options, string eventName, string ruleSetName)
    {
        var existing = options.Triggers.FirstOrDefault(trigger =>
            string.Equals(trigger.Event, eventName, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            options.Triggers.Add(new TaggerRuleTriggerOptions
            {
                Event = eventName,
                RuleSets = { ruleSetName }
            });

            return;
        }

        if (existing.RuleSets.Any(name => string.Equals(name, ruleSetName, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        existing.RuleSets.Add(ruleSetName);
    }
}
