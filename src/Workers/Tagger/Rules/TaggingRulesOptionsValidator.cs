using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;

namespace Tagger;

internal sealed class TaggingRulesOptionsValidator : IValidateOptions<TaggingRulesOptions>
{
    public ValidateOptionsResult Validate(string? name, TaggingRulesOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Rules is null || options.Rules.Count == 0)
        {
            return ValidateOptionsResult.Success;
        }

        var errors = new List<string>();

        for (var index = 0; index < options.Rules.Count; index++)
        {
            var rule = options.Rules[index];
            if (rule is null)
            {
                continue;
            }

            var ruleLabel = string.IsNullOrWhiteSpace(rule.Name) ? $"Rule #{index + 1}" : rule.Name;

            if (rule.TagId == Guid.Empty)
            {
                errors.Add($"{ruleLabel} must specify a tagId.");
            }

            if (rule.Conditions is null)
            {
                continue;
            }

            var invalidConditions = rule.Conditions
                .Where(condition => condition is not null && string.IsNullOrWhiteSpace(condition.Field))
                .ToArray();

            if (invalidConditions.Length > 0)
            {
                errors.Add($"{ruleLabel} contains conditions without a field name.");
            }
        }

        return errors.Count > 0 ? ValidateOptionsResult.Fail(errors) : ValidateOptionsResult.Success;
    }
}
