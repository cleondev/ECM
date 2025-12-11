using Ecm.Rules.Abstractions;
using Ecm.Rules.Engine;

using Tagger.Rules.Custom;

namespace Tagger.Rules.Configuration;

/// <summary>
/// Supplies the default tagging rulesets shipped with the worker so it functions without custom configuration.
/// </summary>
internal sealed class BuiltInRuleProvider : IRuleProvider
{
    private readonly IReadOnlyCollection<IRuleSet> _ruleSets = new[]
    {
        CreateRuleSet(TaggingRuleSetNames.DocumentUploaded),
        CreateRuleSet(TaggingRuleSetNames.OcrCompleted)
    };

    /// <summary>
    /// Identifies the provider when rules are merged within the engine.
    /// </summary>
    public string Source => "Tagger.BuiltIn";

    public IEnumerable<IRuleSet> GetRuleSets() => _ruleSets;

    private static RuleSet CreateRuleSet(string name)
    {
        var rules = new IRule[]
        {
            new AutoDateRule(),
            new DocumentTypeRule()
        };

        return new RuleSet(name, rules);
    }
}
