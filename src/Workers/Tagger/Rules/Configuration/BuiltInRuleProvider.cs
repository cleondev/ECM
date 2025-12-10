using Ecm.Rules.Abstractions;
using Ecm.Rules.Engine;

using Tagger.Rules.Custom;

namespace Tagger.Rules.Configuration;

internal sealed class BuiltInRuleProvider : IRuleProvider
{
    private readonly IReadOnlyCollection<IRuleSet> _ruleSets = new[]
    {
        CreateRuleSet(TaggingRuleSetNames.DocumentUploaded),
        CreateRuleSet(TaggingRuleSetNames.OcrCompleted)
    };

    public string Source => "Tagger.BuiltIn";

    public IEnumerable<IRuleSet> GetRuleSets() => _ruleSets;

    private static IRuleSet CreateRuleSet(string name)
    {
        var rules = new IRule[]
        {
            new AutoDateRule(),
            new DocumentTypeRule()
        };

        return new RuleSet(name, rules);
    }
}
