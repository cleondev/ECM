using System;
using System.Collections.Generic;

namespace Tagger.Rules.Configuration;

public sealed class TaggerRuleTriggerOptions
{
    public string? Event { get; set; }

    public IList<string> RuleSets { get; init; } = new List<string>();
}
