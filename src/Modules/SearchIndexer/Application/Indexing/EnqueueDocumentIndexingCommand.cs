using System;
using System.Collections.Generic;
using ECM.SearchIndexer.Domain.Indexing;

namespace ECM.SearchIndexer.Application.Indexing;

public sealed record EnqueueDocumentIndexingCommand(
    Guid DocumentId,
    string Title,
    string? Summary,
    string? Content,
    IDictionary<string, string>? Metadata,
    IReadOnlyCollection<string>? Tags,
    SearchIndexingType IndexingType);
