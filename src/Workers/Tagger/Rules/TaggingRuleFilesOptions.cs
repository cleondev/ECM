using System.Collections.Generic;

namespace Tagger;

public sealed class TaggingRuleFilesOptions
{
    public const string SectionName = "TaggingRuleFiles";

    public IList<string> Paths { get; init; } = new List<string>();
}
