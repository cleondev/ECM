using System;

namespace Tagger;

public sealed class TaggingRuleConditionOptions
{
    public string Field { get; set; } = string.Empty;

    public TaggingRuleOperator Operator { get; set; } = TaggingRuleOperator.Equals;

    public string? Value { get; set; }

    public string[] Values { get; set; } = Array.Empty<string>();
}
