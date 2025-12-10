using Ecm.Rules.Abstractions;

using Tagger.Rules.Configuration;

namespace Tagger.Rules.Custom;

/// <summary>
/// Derives a tag name using the UTC timestamp of the event that triggered rule evaluation.
/// </summary>
internal sealed class AutoDateRule : IRule
{
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
        output.AddTagName(tagName);
    }
}
