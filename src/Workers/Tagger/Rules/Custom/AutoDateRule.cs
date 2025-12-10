using System;
using System.Linq;

using Ecm.Rules.Abstractions;

using Tagger.Rules.Configuration;

namespace Tagger.Rules.Custom;

internal sealed class AutoDateRule : IRule
{
    private static readonly string[] BasePath = { "LOS", "CreditApplication" };

    public string Name => "Auto Date";

    public bool Match(IRuleContext ctx)
        => ctx.Get("OccurredAtUtc", default(DateTimeOffset)) != default;

    public void Apply(IRuleContext ctx, IRuleOutput output)
    {
        var dt = ctx.Get("OccurredAtUtc", default(DateTimeOffset));
        if (dt == default)
            return;

        output.AddTag(
            TagDefinition.Create(
                dt.ToString("dd"),
                BasePath.Concat(new[] { dt.ToString("yyyy"), dt.ToString("MMMM") }),
                scope: TagScope.Group,
                namespaceDisplayName: TagDefaults.DefaultNamespaceDisplayName,
                color: "#2F855A",
                iconKey: "calendar"));
    }
}
