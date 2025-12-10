using System;
using System.Collections.Generic;
using Ecm.Rules.Providers.Json;

namespace Tagger.Rules.Configuration;

/// <summary>
/// Options that configure how the Tagger worker loads and triggers rule sets.
/// </summary>
public sealed class TaggerRulesOptions
{
    public const string SectionName = "TaggerRules";

    /// <summary>
    /// Optional user identifier recorded as the actor when applying tags on behalf of the worker.
    /// </summary>
    public Guid? AppliedBy { get; set; }

    /// <summary>
    /// Additional JSON files that contain ruleset definitions to merge with inline configuration.
    /// </summary>
    public IList<string> Files { get; init; } = new List<string>();

    /// <summary>
    /// Mapping of integration events to the rule sets that should be evaluated.
    /// </summary>
    public IList<TaggerRuleTriggerOptions> Triggers { get; init; } = new List<TaggerRuleTriggerOptions>();

    /// <summary>
    /// Inline ruleset definitions loaded directly from configuration.
    /// </summary>
    public IList<JsonRuleSetDefinition> RuleSets { get; init; } = new List<JsonRuleSetDefinition>();
}
