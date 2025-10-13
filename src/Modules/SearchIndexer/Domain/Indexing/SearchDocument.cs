using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using ECM.SearchIndexer.Domain.Indexing.Exceptions;

namespace ECM.SearchIndexer.Domain.Indexing;

public sealed class SearchDocument
{
    private SearchDocument(
        Guid documentId,
        string title,
        string aggregatedContent,
        IReadOnlyDictionary<string, string> metadata,
        IReadOnlyCollection<string> tags)
    {
        DocumentId = documentId;
        Title = title;
        AggregatedContent = aggregatedContent;
        Metadata = metadata;
        Tags = tags;
    }

    public Guid DocumentId { get; }

    public string Title { get; }

    public string AggregatedContent { get; }

    public IReadOnlyDictionary<string, string> Metadata { get; }

    public IReadOnlyCollection<string> Tags { get; }

    public static SearchDocument Create(
        Guid documentId,
        string title,
        string? summary,
        string? content,
        IEnumerable<string>? tags,
        IReadOnlyDictionary<string, string>? metadata)
    {
        if (documentId == Guid.Empty)
        {
            throw new InvalidSearchDocumentException("Document id must be a non-empty GUID.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidSearchDocumentException("Title is required for indexing.");
        }

        var normalizedTitle = Normalize(title);
        var normalizedSummary = NormalizeOptional(summary);
        var normalizedContent = NormalizeOptional(content);

        var metadataBuffer = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (metadata is not null)
        {
            foreach (var pair in metadata)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
                {
                    continue;
                }

                metadataBuffer[Normalize(pair.Key)] = Normalize(pair.Value);
            }
        }

        var normalizedTags = tags?
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(Normalize)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();

        var builder = new StringBuilder();
        builder.AppendLine(normalizedTitle);

        if (!string.IsNullOrEmpty(normalizedSummary))
        {
            builder.AppendLine(normalizedSummary);
        }

        if (!string.IsNullOrEmpty(normalizedContent))
        {
            builder.AppendLine(normalizedContent);
        }

        foreach (var pair in metadataBuffer.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"{pair.Key}:{pair.Value}");
        }

        if (normalizedTags.Length > 0)
        {
            builder.AppendLine(string.Join(' ', normalizedTags));
        }

        var aggregated = builder.ToString().Trim();
        if (aggregated.Length == 0)
        {
            throw new InvalidSearchDocumentException("Aggregated content must not be empty.");
        }

        return new SearchDocument(
            documentId,
            normalizedTitle,
            aggregated,
            new ReadOnlyDictionary<string, string>(metadataBuffer),
            new ReadOnlyCollection<string>(normalizedTags));
    }

    public SearchIndexRecord ToRecord(SearchIndexingType indexingType)
    {
        var metadata = Metadata.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.OrdinalIgnoreCase);

        return new SearchIndexRecord(
            DocumentId,
            Title,
            AggregatedContent,
            metadata,
            new List<string>(Tags),
            indexingType);
    }

    private static string Normalize(string value)
        => value.Trim();

    private static string NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
