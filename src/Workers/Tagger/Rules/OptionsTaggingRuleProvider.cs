using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Tagger;

internal sealed class OptionsTaggingRuleProvider : ITaggingRuleProvider
{
    private readonly IOptionsMonitor<TaggingRulesOptions> _options;

    public OptionsTaggingRuleProvider(IOptionsMonitor<TaggingRulesOptions> options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public IReadOnlyCollection<TaggingRuleOptions>? GetRules() => _options.CurrentValue?.Rules;
}
