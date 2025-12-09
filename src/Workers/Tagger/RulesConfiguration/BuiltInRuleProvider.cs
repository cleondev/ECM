using Ecm.Rules.Abstractions;

using Tagger.RulesConfiguration;

namespace Tagger;

internal sealed class BuiltInRuleProvider : IRuleProvider
{
    private readonly IReadOnlyCollection<IRuleSet> _ruleSets = new[]
    {
        AutoDate.CreateRuleSet(TaggingRuleSetNames.DocumentUploaded),
        AutoDate.CreateRuleSet(TaggingRuleSetNames.OcrCompleted),
        DocumentType.CreateRuleSet(TaggingRuleSetNames.DocumentUploaded),
        DocumentType.CreateRuleSet(TaggingRuleSetNames.OcrCompleted)
    };

    public string Source => "Tagger.BuiltIn";

    public IEnumerable<IRuleSet> GetRuleSets() => _ruleSets;
}
