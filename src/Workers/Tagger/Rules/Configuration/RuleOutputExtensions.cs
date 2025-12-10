using Ecm.Rules.Abstractions;

namespace Tagger.Rules.Configuration;

internal static class RuleOutputExtensions
{
    public static void AddTagName(this IRuleOutput output, string tag)
    {
        ArgumentNullException.ThrowIfNull(output);

        if (string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        var tags = GetOrCreateTagSet(output);
        tags.Add(tag.Trim());
        output.Set("TagNames", tags);
    }

    private static HashSet<string> GetOrCreateTagSet(IRuleOutput output)
    {
        if (output.TryGet<HashSet<string>>("TagNames", out var tagSet))
        {
            return tagSet;
        }

        return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
