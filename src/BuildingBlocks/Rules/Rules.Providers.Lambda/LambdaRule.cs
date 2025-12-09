using Ecm.Rules.Abstractions;

namespace Ecm.Rules.Providers.Lambda;

public sealed class LambdaRule : IRule
{
    private readonly Action<IRuleContext, IRuleOutput> _apply;
    private readonly Func<IRuleContext, bool> _match;

    public LambdaRule(
        string name,
        Func<IRuleContext, bool> match,
        Action<IRuleContext, IRuleOutput> apply)
    {
        Name = name;
        _match = match ?? throw new ArgumentNullException(nameof(match));
        _apply = apply ?? throw new ArgumentNullException(nameof(apply));
    }

    public string Name { get; }

    public bool Match(IRuleContext ctx) => _match(ctx);

    public void Apply(IRuleContext ctx, IRuleOutput output) => _apply(ctx, output);
}
