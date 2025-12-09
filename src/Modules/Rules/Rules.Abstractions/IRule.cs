namespace Ecm.Rules.Abstractions;

public interface IRule
{
    string Name { get; }

    bool Match(IRuleContext ctx);
    void Apply(IRuleContext ctx, IRuleOutput output);
}
