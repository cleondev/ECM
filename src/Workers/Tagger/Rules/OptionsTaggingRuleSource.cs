using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Tagger;

internal sealed class OptionsTaggingRuleSource : ITaggingRuleSource
{
    private readonly IOptionsMonitor<TaggingRulesOptions> _options;

    public OptionsTaggingRuleSource(IOptionsMonitor<TaggingRulesOptions> options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public IReadOnlyCollection<TaggingRuleOptions> GetRules() => _options.CurrentValue?.Rules ?? Array.Empty<TaggingRuleOptions>();
}
