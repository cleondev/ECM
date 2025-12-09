namespace Tagger;

internal interface ITaggingRuleSource
{
    IReadOnlyCollection<TaggingRuleOptions> GetRules();
}
