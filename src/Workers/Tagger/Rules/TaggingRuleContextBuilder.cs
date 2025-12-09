using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Tagger;

internal sealed class TaggingRuleContextBuilder
{
    private readonly Dictionary<string, string> _fields;
    private readonly Dictionary<string, string> _metadata;

    private TaggingRuleContextBuilder(Guid documentId, string title)
    {
        DocumentId = documentId;
        Title = title;

        _metadata = new(StringComparer.OrdinalIgnoreCase);
        _fields = new(_metadata, StringComparer.OrdinalIgnoreCase);

        AddField("title", title);
    }

    public Guid DocumentId { get; }

    public string Title { get; }

    public string? Summary { get; private set; }

    public string? Content { get; private set; }

    public TaggingRuleContextBuilder WithSummary(string? summary)
    {
        if (!string.IsNullOrWhiteSpace(summary))
        {
            Summary = summary;
            AddField("summary", summary);
        }

        return this;
    }

    public TaggingRuleContextBuilder WithContent(string? content)
    {
        if (!string.IsNullOrWhiteSpace(content))
        {
            Content = content;
            AddField("content", content);
        }

        return this;
    }

    public TaggingRuleContextBuilder AddMetadata(IDictionary<string, string>? metadata)
    {
        if (metadata is null)
        {
            return this;
        }

        foreach (var entry in metadata)
        {
            AddMetadata(entry.Key, entry.Value);
        }

        return this;
    }

    public TaggingRuleContextBuilder AddMetadata(string? key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
        {
            return this;
        }

        var normalizedKey = key.Trim();
        var normalizedValue = value.Trim();

        _metadata[normalizedKey] = normalizedValue;
        AddField(normalizedKey, normalizedValue);

        return this;
    }

    public TaggingRuleContextBuilder AddField(string? key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
        {
            return this;
        }

        _fields[key.Trim()] = value.Trim();
        return this;
    }

    public TaggingRuleContext Build()
    {
        return new TaggingRuleContext(
            DocumentId,
            Title,
            Summary,
            Content,
            new ReadOnlyDictionary<string, string>(_metadata),
            new ReadOnlyDictionary<string, string>(_fields));
    }

    public static TaggingRuleContextBuilder FromMetadata(
        Guid documentId,
        string title,
        string? summary,
        string? content,
        IDictionary<string, string>? metadata)
    {
        var builder = new TaggingRuleContextBuilder(documentId, title)
            .WithSummary(summary)
            .WithContent(content)
            .AddMetadata(metadata);

        return builder;
    }

    public static TaggingRuleContextBuilder FromIntegrationEvent(ITaggingIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return FromMetadata(
            integrationEvent.DocumentId,
            integrationEvent.Title,
            integrationEvent.Summary,
            integrationEvent.Content,
            integrationEvent.Metadata);
    }
}
