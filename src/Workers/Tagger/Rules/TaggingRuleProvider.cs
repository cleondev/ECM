using System.Text.RegularExpressions;
using Ecm.Rules.Abstractions;
using Ecm.Rules.Engine;
using Ecm.Rules.Providers.Lambda;

namespace Tagger;

internal sealed class TaggingRuleProvider : IRuleProvider
{
    private static readonly StringComparison Comparison = StringComparison.OrdinalIgnoreCase;
    private readonly IEnumerable<ITaggingRuleSource> _sources;

    public TaggingRuleProvider(IEnumerable<ITaggingRuleSource> sources)
    {
        _sources = sources ?? throw new ArgumentNullException(nameof(sources));
    }

    public string Source => "Tagger.Configuration";

    public IEnumerable<IRuleSet> GetRuleSets()
    {
        var rules = _sources
            .SelectMany(source => source.GetRules() ?? Array.Empty<TaggingRuleOptions>())
            .Where(IsRuleEligible)
            .ToArray();

        if (rules.Length == 0)
        {
            return Array.Empty<IRuleSet>();
        }

        var ruleSets = new List<IRuleSet>();

        var uploadRules = rules
            .Where(rule => rule.Trigger is TaggingRuleTrigger.All or TaggingRuleTrigger.DocumentUploaded)
            .ToArray();
        if (uploadRules.Length > 0)
        {
            ruleSets.Add(BuildRuleSet(TaggingRuleSetNames.DocumentUploaded, uploadRules));
        }

        var ocrRules = rules
            .Where(rule => rule.Trigger is TaggingRuleTrigger.All or TaggingRuleTrigger.OcrCompleted)
            .ToArray();
        if (ocrRules.Length > 0)
        {
            ruleSets.Add(BuildRuleSet(TaggingRuleSetNames.OcrCompleted, ocrRules));
        }

        return ruleSets;
    }

    private static bool IsRuleEligible(TaggingRuleOptions? rule)
    {
        return rule is not null && rule.Enabled && rule.TagId != Guid.Empty;
    }

    private static IRuleSet BuildRuleSet(string name, IEnumerable<TaggingRuleOptions> rules)
    {
        var builder = new LambdaRuleSetBuilder();

        foreach (var rule in rules)
        {
            var label = string.IsNullOrWhiteSpace(rule.Name) ? rule.TagId.ToString() : rule.Name;
            builder.Add(label, ctx => DoesRuleMatch(rule, ctx), (_, output) => Apply(rule.TagId, output));
        }

        return builder.Build(name);
    }

    private static bool DoesRuleMatch(TaggingRuleOptions rule, IRuleContext context)
    {
        if (rule.Conditions is null || rule.Conditions.Count == 0)
        {
            return true;
        }

        var fields = context.Get<IReadOnlyDictionary<string, string>>("Fields", new Dictionary<string, string>());

        var predicates = rule.Conditions
            .Select(condition => EvaluateCondition(condition, fields))
            .ToArray();

        return rule.Match switch
        {
            TaggingRuleMatchMode.Any => predicates.Any(result => result),
            _ => predicates.All(result => result)
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

    private static void Apply(Guid tagId, IRuleOutput output)
    {
        if (!output.TryGet("TagIds", out List<Guid>? tags) || tags is null)
        {
            tags = new List<Guid>();
        }

        if (!tags.Contains(tagId))
        {
            tags.Add(tagId);
        }

        output.Set("TagIds", tags);
    }
}
