using Ecm.Rules.Abstractions;

namespace Ecm.Rules.Providers.Lambda;

public sealed class LambdaRuleProvider : IRuleProvider
{
    private readonly IReadOnlyCollection<IRuleSet> _ruleSets;

    public LambdaRuleProvider(IEnumerable<IRuleSet> ruleSets)
    {
        _ruleSets = ruleSets?.ToArray() ?? throw new ArgumentNullException(nameof(ruleSets));
    }

    public string Source => "Lambda";

    public IEnumerable<IRuleSet> GetRuleSets() => _ruleSets;
}
