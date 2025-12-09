using System;
using System.Collections.Generic;
using Ecm.Rules.Providers.Json;

namespace Tagger;

public sealed class TaggerRulesOptions
{
    public const string SectionName = "TaggerRules";

    public Guid? AppliedBy { get; set; }

    public IList<string> Files { get; init; } = new List<string>();

    public IList<TaggerRuleTriggerOptions> Triggers { get; init; } = new List<TaggerRuleTriggerOptions>();

    public IList<JsonRuleSetDefinition> RuleSets { get; init; } = new List<JsonRuleSetDefinition>();
}
