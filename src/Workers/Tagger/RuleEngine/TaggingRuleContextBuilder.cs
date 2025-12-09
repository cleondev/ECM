using System.Collections.ObjectModel;

namespace Tagger;

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

    public TaggingRuleContextBuilder AddField(string? key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
        {
            return this;
        }

        _fields[key.Trim()] = value.Trim();
        return this;
    }

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

    public static TaggingRuleContextBuilder FromIntegrationEvent(ITaggingIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return FromMetadata(
                integrationEvent.DocumentId,
                integrationEvent.Title,
                integrationEvent.OccurredAtUtc,
                integrationEvent.Summary,
                integrationEvent.Content,
                integrationEvent.Metadata)
            .WithEvent(TaggingIntegrationEventNames.FromEvent(integrationEvent));
    }
}
