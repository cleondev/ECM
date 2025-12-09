using Ecm.Rules.Abstractions;
using Ecm.Rules.Providers.Lambda;

using Tagger.RulesConfiguration;

namespace Tagger;

internal static class AutoDate
{
    public static IRuleSet CreateRuleSet(string ruleSetName)
    {
        var builder = new LambdaRuleSetBuilder();

        builder.Add("Auto Date", _ => true, Apply);

        return builder.Build(ruleSetName);
    }

    private static void Apply(IRuleContext context, IRuleOutput output)
    {
        var occurredAtUtc = context.Get("OccurredAtUtc", default(DateTimeOffset));
        if (occurredAtUtc == default)
        {
            return;
        }

        var tagName = $"Uploaded {occurredAtUtc:yyyy-MM-dd}";
        output.AddTagName(tagName);
    }
}
