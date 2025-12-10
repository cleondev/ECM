using System.Collections.Generic;

using Ecm.Rules.Abstractions;

using Tagger.Rules.Configuration;

namespace Tagger.Rules.Custom;

/// <summary>
/// Derives a tag name using the UTC timestamp of the event that triggered rule evaluation.
/// </summary>
internal sealed class AutoDateRule : IRule
{
    private static readonly IReadOnlyList<string> DatePathSegments = new[]
    {
        "LOS",
        "CreditApplication",
        "Dates",
    };

    public string Name => "Auto Date";

    /// <summary>
    /// Matches when the event includes a non-default <c>OccurredAtUtc</c> value.
    /// </summary>
    public bool Match(IRuleContext ctx)
    {
        var occurredAtUtc = ctx.Get("OccurredAtUtc", default(DateTimeOffset));
        return occurredAtUtc != default;
    }

    /// <summary>
    /// Emits a tag name based on the event timestamp in <c>yyyy-MM-dd</c> format.
    /// </summary>
    public void Apply(IRuleContext ctx, IRuleOutput output)
    {
        var occurredAtUtc = ctx.Get("OccurredAtUtc", default(DateTimeOffset));
        if (occurredAtUtc == default)
        {
            return;
        }

        var tagName = $"Uploaded {occurredAtUtc:yyyy-MM-dd}";

        output.AddTag(
            TagDefinition.Create(
                tagName,
                DatePathSegments,
                scope: TagScope.Group,
                namespaceDisplayName: TagDefaults.DefaultNamespaceDisplayName,
                color: "#2F855A",
                iconKey: "calendar"));
    }
}
