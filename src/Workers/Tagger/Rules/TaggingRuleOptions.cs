using System;
using System.Collections.Generic;

namespace Tagger;

public sealed class TaggingRuleOptions
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool Enabled { get; set; } = true;

    public Guid TagId { get; set; }

    public TaggingRuleMatchMode Match { get; set; } = TaggingRuleMatchMode.All;

    public TaggingRuleTrigger Trigger { get; set; } = TaggingRuleTrigger.All;

    public IList<TaggingRuleConditionOptions> Conditions { get; init; } = new List<TaggingRuleConditionOptions>();
}
