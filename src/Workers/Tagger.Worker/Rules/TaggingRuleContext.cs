using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Tagger;

public sealed class TaggingRuleContext
{
    private TaggingRuleContext(
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
        var normalizedMetadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : Normalize(metadata);

        var fieldBag = new Dictionary<string, string>(normalizedMetadata, StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(title))
        {
            fieldBag["title"] = title;
        }

        if (!string.IsNullOrWhiteSpace(summary))
        {
            fieldBag["summary"] = summary!;
        }

        if (!string.IsNullOrWhiteSpace(content))
        {
            fieldBag["content"] = content!;
        }

        return new TaggingRuleContext(
            documentId,
            title,
            summary,
            content,
            new ReadOnlyDictionary<string, string>(normalizedMetadata),
            new ReadOnlyDictionary<string, string>(fieldBag));
    }

    private static Dictionary<string, string> Normalize(IDictionary<string, string> metadata)
    {
        var buffer = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in metadata)
        {
            var key = entry.Key?.Trim();
            var value = entry.Value?.Trim();

            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
            {
                continue;
            }

            buffer[key] = value;
        }

        return buffer;
    }
}
