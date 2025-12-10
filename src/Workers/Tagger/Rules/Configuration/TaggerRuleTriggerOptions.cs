using System;
using System.Collections.Generic;

namespace Tagger.Rules.Configuration;

/// <summary>
/// Maps a tagging integration event to the rule sets that must run when the event is processed.
/// </summary>
public sealed class TaggerRuleTriggerOptions
{
    /// <summary>
    /// Name of the integration event (for example, <c>DocumentUploaded</c> or <c>OcrCompleted</c>).
    /// </summary>
    public string? Event { get; set; }

    /// <summary>
    /// Rule set names to execute when the event fires.
    /// </summary>
    public IList<string> RuleSets { get; init; } = new List<string>();
}
