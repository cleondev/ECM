using Ecm.Rules.Abstractions;

namespace Ecm.Rules.Engine;

public sealed class RuleSet : IRuleSet
{
    public string Name { get; }
    public IReadOnlyCollection<IRule> Rules { get; }

    public RuleSet(string name, IEnumerable<IRule> rules)
    {
        Name = name;
        Rules = rules.ToArray();
    }
}
