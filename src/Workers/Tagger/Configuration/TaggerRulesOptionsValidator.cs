using System;
using System.Collections.Generic;
using System.Linq;
using Ecm.Rules.Providers.Json;
using Microsoft.Extensions.Options;

namespace Tagger;

internal sealed class TaggerRulesOptionsValidator : IValidateOptions<TaggerRulesOptions>
{
    public ValidateOptionsResult Validate(string? name, TaggerRulesOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var errors = new List<string>();

        if (options.RuleSets is not null)
        {
            errors.AddRange(ValidateRuleSets(options.RuleSets));
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }

    private static IEnumerable<string> ValidateRuleSets(IEnumerable<JsonRuleSetDefinition> definitions)
    {
        var index = 0;

        foreach (var definition in definitions)
        {
            index++;

            if (definition is null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(definition.Name))
            {
                yield return $"Rule set #{index} must specify a name.";
            }

            if (definition.Rules is null)
            {
                continue;
            }

            var missingRuleNames = definition.Rules
                .Select((rule, idx) => (rule, idx))
                .Where(tuple => tuple.rule is not null && string.IsNullOrWhiteSpace(tuple.rule.Name))
                .Select(tuple => tuple.idx + 1)
                .ToArray();

            if (missingRuleNames.Length > 0)
            {
                yield return $"Rule set '{definition.Name}' contains rules without names at positions {string.Join(", ", missingRuleNames)}.";
            }
        }
    }
}
