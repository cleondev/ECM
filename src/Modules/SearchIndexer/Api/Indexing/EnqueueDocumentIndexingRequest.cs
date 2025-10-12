using System;
using System.Collections.Generic;
using ECM.SearchIndexer.Application.Indexing;

namespace ECM.SearchIndexer.Api.Indexing;

public sealed record EnqueueDocumentIndexingRequest
{
    public Guid DocumentId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Summary { get; init; }

    public string? Content { get; init; }

    public IDictionary<string, string>? Metadata { get; init; }

    public IReadOnlyCollection<string>? Tags { get; init; }

    public EnqueueDocumentIndexingCommand ToCommand()
        => new(
            DocumentId,
            Title,
            Summary,
            Content,
            Metadata,
            Tags);
}
