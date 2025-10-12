using System;
using System.Collections.Generic;
using System.Linq;
using ECM.SearchIndexer.Domain.Indexing;

namespace ECM.SearchIndexer.Api.Indexing;

public sealed record SearchIndexDocumentResponse
{
    public Guid DocumentId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public IDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    public IReadOnlyCollection<string> Tags { get; init; } = Array.Empty<string>();

    public static SearchIndexDocumentResponse FromRecord(SearchIndexRecord record)
        => new()
        {
            DocumentId = record.DocumentId,
            Title = record.Title,
            Content = record.Content,
            Metadata = new Dictionary<string, string>(record.Metadata),
            Tags = record.Tags.ToArray()
        };
}
