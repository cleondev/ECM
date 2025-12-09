using System.Collections.Generic;

namespace Tagger;

internal interface ITaggingRuleProvider
{
    IReadOnlyCollection<TaggingRuleOptions>? GetRules();
}
