using Ecm.Rules.Abstractions;

namespace Ecm.Rules.Providers.Hardcoded;

public sealed class HardcodedRuleProvider : IRuleProvider
{
    private readonly IEnumerable<IRuleSet> _ruleSets;

    public HardcodedRuleProvider(IEnumerable<IRuleSet> ruleSets)
    {
        _ruleSets = ruleSets ?? throw new ArgumentNullException(nameof(ruleSets));
    }

    public string Source => "Hardcoded";

    public IEnumerable<IRuleSet> GetRuleSets() => _ruleSets;
}
