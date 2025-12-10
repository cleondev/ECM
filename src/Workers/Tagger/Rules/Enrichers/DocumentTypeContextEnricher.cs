using Tagger.Events;
using Tagger.Rules.Configuration;
using Tagger.Rules.Custom;

namespace Tagger.Rules.Enrichers;

internal sealed class DocumentTypeContextEnricher : ITaggingRuleContextEnricher
{
    public void Enrich(TaggingRuleContextBuilder builder, ITaggingIntegrationEvent integrationEvent)
    {
        var extension = DocumentType.ResolveExtension(integrationEvent);
        if (!string.IsNullOrWhiteSpace(extension))
        {
            builder.AddField("extension", extension);
        }
    }
}
