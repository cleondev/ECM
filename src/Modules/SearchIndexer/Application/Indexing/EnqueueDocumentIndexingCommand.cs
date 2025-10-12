using System;
using System.Collections.Generic;

namespace ECM.SearchIndexer.Application.Indexing;

public sealed record EnqueueDocumentIndexingCommand(
    Guid DocumentId,
    string Title,
    string? Summary,
    string? Content,
    IDictionary<string, string>? Metadata,
    IReadOnlyCollection<string>? Tags);
