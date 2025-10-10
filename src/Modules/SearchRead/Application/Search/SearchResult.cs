using System;

namespace ECM.SearchRead.Application.Search;

public sealed record SearchResult(Guid DocumentId, string Title, double Score);
