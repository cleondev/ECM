using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;

namespace Tagger;

internal sealed class TaggingRuleFilesOptionsValidator : IValidateOptions<TaggingRuleFilesOptions>
{
    public ValidateOptionsResult Validate(string? name, TaggingRuleFilesOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Paths is null || options.Paths.Count == 0)
        {
            return ValidateOptionsResult.Success;
        }

        var invalid = options.Paths
            .Where(string.IsNullOrWhiteSpace)
            .ToArray();

        if (invalid.Length > 0)
        {
            return ValidateOptionsResult.Fail("Tagging rule file paths cannot be empty.");
        }

        return ValidateOptionsResult.Success;
    }
}
