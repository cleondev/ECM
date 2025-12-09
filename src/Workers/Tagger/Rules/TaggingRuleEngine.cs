using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Tagger;

internal sealed class TaggingRuleEngine : ITaggingRuleEngine
{
    private static readonly StringComparison Comparison = StringComparison.OrdinalIgnoreCase;
    private readonly IReadOnlyCollection<ITaggingRuleProvider> _ruleProviders;

    public TaggingRuleEngine(IEnumerable<ITaggingRuleProvider> ruleProviders)
    {
        _ruleProviders = ruleProviders?.ToArray()
            ?? throw new ArgumentNullException(nameof(ruleProviders));
    }

    public IReadOnlyCollection<Guid> Evaluate(TaggingRuleContext context, TaggingRuleTrigger trigger)
    {
        ArgumentNullException.ThrowIfNull(context);

        var rules = _ruleProviders
            .SelectMany(provider => provider.GetRules() ?? Array.Empty<TaggingRuleOptions>())
            .ToArray();

        if (rules.Length == 0)
        {
            return Array.Empty<Guid>();
        }

        var matches = new HashSet<Guid>();

        foreach (var rule in rules)
        {
            if (!IsRuleEligible(rule, trigger))
            {
                continue;
            }

            if (!DoesRuleMatch(rule, context.Fields))
            {
                continue;
            }

            matches.Add(rule.TagId);
        }

        return matches.Count == 0 ? Array.Empty<Guid>() : matches.ToArray();
    }

    private static bool IsRuleEligible(TaggingRuleOptions? rule, TaggingRuleTrigger trigger)
    {
        if (rule is null || !rule.Enabled || rule.TagId == Guid.Empty)
        {
            return false;
        }

        if (rule.Trigger == TaggingRuleTrigger.All || rule.Trigger == trigger)
        {
            return true;
        }

        return false;
    }

    private static bool DoesRuleMatch(TaggingRuleOptions rule, IReadOnlyDictionary<string, string> fields)
    {
        if (rule.Conditions is null || rule.Conditions.Count == 0)
        {
            return true;
        }

        var predicates = rule.Conditions
            .Select(condition => (condition, result: EvaluateCondition(condition, fields)))
            .ToArray();

        return rule.Match switch
        {
            TaggingRuleMatchMode.Any => predicates.Any(tuple => tuple.result),
            _ => predicates.All(tuple => tuple.result)
        };
    }

    private static bool EvaluateCondition(TaggingRuleConditionOptions? condition, IReadOnlyDictionary<string, string> fields)
    {
        if (condition is null)
        {
            return false;
        }

        var fieldName = condition.Field?.Trim();
        if (string.IsNullOrEmpty(fieldName))
        {
            return false;
        }

        if (!fields.TryGetValue(fieldName, out var actual) || string.IsNullOrWhiteSpace(actual))
        {
            return false;
        }

        var expectedValues = ExtractValues(condition);
        if (expectedValues.Count == 0 && condition.Operator != TaggingRuleOperator.Regex)
        {
            return false;
        }

        return condition.Operator switch
        {
            TaggingRuleOperator.Equals => expectedValues.Any(value => string.Equals(actual, value, Comparison)),
            TaggingRuleOperator.NotEquals => expectedValues.All(value => !string.Equals(actual, value, Comparison)),
            TaggingRuleOperator.Contains => expectedValues.Any(value => actual.Contains(value, Comparison)),
            TaggingRuleOperator.NotContains => expectedValues.All(value => !actual.Contains(value, Comparison)),
            TaggingRuleOperator.StartsWith => expectedValues.Any(value => actual.StartsWith(value, Comparison)),
            TaggingRuleOperator.EndsWith => expectedValues.Any(value => actual.EndsWith(value, Comparison)),
            TaggingRuleOperator.In => expectedValues.Any(value => string.Equals(actual, value, Comparison)),
            TaggingRuleOperator.NotIn => expectedValues.All(value => !string.Equals(actual, value, Comparison)),
            TaggingRuleOperator.Regex => MatchesRegex(condition, actual),
            _ => false
        };
    }

    private static IReadOnlyList<string> ExtractValues(TaggingRuleConditionOptions condition)
    {
        if (condition.Values is { Length: > 0 })
        {
            return condition.Values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .ToArray();
        }

        if (!string.IsNullOrWhiteSpace(condition.Value))
        {
            return new[] { condition.Value.Trim() };
        }

        return Array.Empty<string>();
    }

    private static bool MatchesRegex(TaggingRuleConditionOptions condition, string actual)
    {
        var pattern = condition.Value;
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        try
        {
            return Regex.IsMatch(actual, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
