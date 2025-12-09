namespace Ecm.Rules.Abstractions;

public interface IRuleSet
{
    string Name { get; }
    IReadOnlyCollection<IRule> Rules { get; }
}
