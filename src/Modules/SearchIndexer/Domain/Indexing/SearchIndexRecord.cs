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

    public SearchIndexingType IndexingType { get; init; } = SearchIndexingType.Basic;

    public SearchIndexRecord()
    {
    }

    public SearchIndexRecord(
        Guid documentId,
        string title,
        string content,
        IDictionary<string, string> metadata,
        IList<string> tags,
        SearchIndexingType indexingType)
    {
        DocumentId = documentId;
        Title = title;
        Content = content;
        Metadata = metadata;
        Tags = tags;
        IndexingType = indexingType;
    }
}
