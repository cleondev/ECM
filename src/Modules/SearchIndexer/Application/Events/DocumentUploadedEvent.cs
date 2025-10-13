using System;
using System.Collections.Generic;

namespace ECM.SearchIndexer.Application.Events;

public sealed record DocumentUploadedEvent(
    Guid DocumentId,
    string Title,
    string? Summary,
    string? Content,
    IDictionary<string, string>? Metadata,
    IReadOnlyCollection<string>? Tags);
