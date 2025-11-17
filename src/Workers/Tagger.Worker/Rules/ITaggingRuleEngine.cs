using System;
using System.Collections.Generic;

namespace Tagger;

internal interface ITaggingRuleEngine
{
    IReadOnlyCollection<Guid> Evaluate(TaggingRuleContext context, TaggingRuleTrigger trigger);
}
