using Ecm.Rules.Abstractions;
using Ecm.Rules.Engine;

namespace Ecm.Rules.Providers.Lambda;

public sealed class LambdaRuleSetBuilder
{
    private readonly List<IRule> _rules = new();

    public LambdaRuleSetBuilder Add(
        string name,
        Func<IRuleContext, bool> match,
        Action<IRuleContext, IRuleOutput> apply)
    {
        _rules.Add(new LambdaRule(name, match, apply));
        return this;
    }

    public IRuleSet Build(string ruleSetName) => new RuleSet(ruleSetName, _rules);
}
