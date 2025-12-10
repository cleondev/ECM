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

        AddTag(output, TagDefinition.Create(tag));
    }

    public static void AddTag(this IRuleOutput output, TagDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(definition);

        var tagSet = GetOrCreateTagList(output);
        tagSet.Add(definition);
        output.Set("Tags", tagSet);
    }

    private static HashSet<TagDefinition> GetOrCreateTagList(IRuleOutput output)
    {
        if (output.TryGet<HashSet<TagDefinition>>("Tags", out var tagSet))
        {
            return tagSet;
        }

        return new HashSet<TagDefinition>(TagDefinition.Comparer);
    }
}
