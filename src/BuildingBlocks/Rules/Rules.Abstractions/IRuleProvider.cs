namespace Ecm.Rules.Abstractions;

public interface IRuleProvider
{
    string Source { get; }

    IEnumerable<IRuleSet> GetRuleSets();
}
