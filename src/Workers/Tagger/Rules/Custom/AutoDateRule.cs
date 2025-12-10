using Ecm.Rules.Abstractions;

using Tagger.Rules.Configuration;

namespace Tagger.Rules.Custom;

internal sealed class AutoDateRule : IRule
{
    public string Name => "Auto Date";

    public bool Match(IRuleContext ctx)
    {
        var occurredAtUtc = ctx.Get("OccurredAtUtc", default(DateTimeOffset));
        return occurredAtUtc != default;
    }

    public void Apply(IRuleContext ctx, IRuleOutput output)
    {
        var occurredAtUtc = ctx.Get("OccurredAtUtc", default(DateTimeOffset));
        if (occurredAtUtc == default)
        {
            return;
        }

        var tagName = $"Uploaded {occurredAtUtc:yyyy-MM-dd}";
        output.AddTagName(tagName);
    }
}
