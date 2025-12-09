using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Tagger;

public sealed class TaggingRuleContext
{
    internal TaggingRuleContext(
        Guid documentId,
        string title,
        string? summary,
        string? content,
        IReadOnlyDictionary<string, string> metadata,
        IReadOnlyDictionary<string, string> fields)
    {
        DocumentId = documentId;
        Title = title;
        Summary = summary;
        Content = content;
        Metadata = metadata;
        Fields = fields;
    }

    public Guid DocumentId { get; }

    public string Title { get; }

    public string? Summary { get; }

    public string? Content { get; }

    public IReadOnlyDictionary<string, string> Metadata { get; }

    public IReadOnlyDictionary<string, string> Fields { get; }

    public static TaggingRuleContext Create(
        Guid documentId,
        string title,
        string? summary,
        string? content,
        IDictionary<string, string>? metadata)
    {
        var builder = TaggingRuleContextBuilder
            .FromMetadata(documentId, title, summary, content, metadata);

        return builder.Build();
    }
}
