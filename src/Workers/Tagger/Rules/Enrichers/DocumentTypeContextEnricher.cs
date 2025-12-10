using Tagger.Events;
using Tagger.Rules.Configuration;
using Tagger.Rules.Custom;

namespace Tagger.Rules.Enrichers;

/// <summary>
/// Populates the rule context with file extension information derived from the tagging event.
/// </summary>
internal sealed class DocumentTypeContextEnricher : ITaggingRuleContextEnricher
{
    /// <summary>
    /// Adds an <c>extension</c> field to the context when it can be resolved from metadata or the title.
    /// </summary>
    public void Enrich(TaggingRuleContextBuilder builder, ITaggingIntegrationEvent integrationEvent)
    {
        var extension = DocumentTypeRule.ResolveExtension(integrationEvent);
        if (!string.IsNullOrWhiteSpace(extension))
        {
            builder.AddField("extension", extension);
        }
    }
}
