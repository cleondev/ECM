using System;
using System.Collections.Generic;

namespace Tagger;

public sealed class TaggingRulesOptions
{
    public const string SectionName = "TaggingRules";

    /// <summary>
    ///     Optional identifier of the system account recorded as the actor when rules assign tags.
    ///     When null, assignments will be treated as automated.
    /// </summary>
    public Guid? AppliedBy { get; set; }

    public IList<TaggingRuleOptions> Rules { get; init; } = new List<TaggingRuleOptions>();
}
