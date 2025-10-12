using System;

namespace ECM.SearchIndexer.Application.Indexing;

public sealed record EnqueueDocumentIndexingResult(string JobId, Guid DocumentId);
