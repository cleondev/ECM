using System.Collections.ObjectModel;

using Tagger.Events;

namespace Tagger.Rules.Configuration;

/// <summary>
/// Builds the dictionary passed into the rules engine so rule expressions can access event metadata consistently.
/// </summary>
internal sealed class TaggingRuleContextBuilder
{
    private readonly Dictionary<string, object> _items;
    private readonly Dictionary<string, string> _metadata;
    private readonly Dictionary<string, string> _fields;

    private TaggingRuleContextBuilder(Guid documentId, string title, DateTimeOffset occurredAtUtc)
    {
        DocumentId = documentId;
        Title = title;
        OccurredAtUtc = occurredAtUtc;

        _items = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["DocumentId"] = documentId,
            ["Title"] = title,
            ["OccurredAtUtc"] = occurredAtUtc
        };

        _metadata = new(StringComparer.OrdinalIgnoreCase);
        _fields = new Dictionary<string, string>(_metadata, StringComparer.OrdinalIgnoreCase);

        AddField("title", title);
        AddField("occurredAtUtc", occurredAtUtc.ToString("O"));
        AddField("occurredAtDate", occurredAtUtc.ToString("yyyy-MM-dd"));
    }

    public Guid DocumentId { get; }

    public string Title { get; }

    public DateTimeOffset OccurredAtUtc { get; }

    public string? Summary { get; private set; }

    public string? Content { get; private set; }

    public string? EventName { get; private set; }

    /// <summary>
    /// Adds a human-readable summary from the event to the rule context if present.
    /// </summary>
    public TaggingRuleContextBuilder WithSummary(string? summary)
    {
        if (!string.IsNullOrWhiteSpace(summary))
        {
            Summary = summary;
            _items["Summary"] = summary;
            AddField("summary", summary);
        }

        return this;
    }

    /// <summary>
    /// Adds content or OCR text to the rule context when available.
    /// </summary>
    public TaggingRuleContextBuilder WithContent(string? content)
    {
        if (!string.IsNullOrWhiteSpace(content))
        {
            Content = content;
            _items["Content"] = content;
            AddField("content", content);
        }

        return this;
    }

    /// <summary>
    /// Adds all metadata key/value pairs from the integration event to the context.
    /// </summary>
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

    /// <summary>
    /// Adds a single metadata entry into the context and derived fields dictionary.
    /// </summary>
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

    /// <summary>
    /// Records the logical event name so rules can branch on which integration event fired.
    /// </summary>
    public TaggingRuleContextBuilder WithEvent(string eventName)
    {
        if (!string.IsNullOrWhiteSpace(eventName))
        {
            EventName = eventName.Trim();
            _items["EventName"] = EventName;
            AddField("eventName", EventName);
        }

        return this;
    }

    /// <summary>
    /// Adds a derived field for use in rule expressions; null/empty values are ignored.
    /// </summary>
    public TaggingRuleContextBuilder AddField(string? key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
        {
            return this;
        }

        _fields[key.Trim()] = value.Trim();
        return this;
    }

    /// <summary>
    /// Finalizes the context, exposing metadata, derived fields, and base properties as a read-only dictionary.
    /// </summary>
    public IReadOnlyDictionary<string, object> Build()
    {
        _items["Metadata"] = new ReadOnlyDictionary<string, string>(_metadata);
        _items["Fields"] = new ReadOnlyDictionary<string, string>(_fields);

        foreach (var field in _fields)
        {
            _items[field.Key] = field.Value;
        }

        return new ReadOnlyDictionary<string, object>(_items);
    }

    /// <summary>
    /// Seeds the builder from raw metadata values commonly present on integration events.
    /// </summary>
    public static TaggingRuleContextBuilder FromMetadata(
        Guid documentId,
        string title,
        DateTimeOffset occurredAtUtc,
        string? summary,
        string? content,
        IDictionary<string, string>? metadata)
    {
        var builder = new TaggingRuleContextBuilder(documentId, title, occurredAtUtc)
            .WithSummary(summary)
            .WithContent(content)
            .AddMetadata(metadata);

        return builder;
    }

    /// <summary>
    /// Creates a builder from any tagging integration event, mapping common fields into context entries.
    /// </summary>
    public static TaggingRuleContextBuilder FromIntegrationEvent(ITaggingIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return FromMetadata(
            integrationEvent.DocumentId,
            integrationEvent.Title,
            integrationEvent.OccurredAtUtc,
            integrationEvent.Summary,
            integrationEvent.Content,
            integrationEvent.Metadata);
    }
}
