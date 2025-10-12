using System;
using System.Collections.Generic;

namespace ECM.SearchIndexer.Domain.Indexing;

public sealed record class SearchIndexRecord
{
    public Guid DocumentId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public IDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    public IList<string> Tags { get; init; } = new List<string>();

    public SearchIndexRecord()
    {
    }

    public SearchIndexRecord(Guid documentId, string title, string content, IDictionary<string, string> metadata, IList<string> tags)
    {
        DocumentId = documentId;
        Title = title;
        Content = content;
        Metadata = metadata;
        Tags = tags;
    }
}
