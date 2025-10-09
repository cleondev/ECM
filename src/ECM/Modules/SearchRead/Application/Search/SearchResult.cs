using System;

namespace ECM.Modules.SearchRead.Application.Search;

public sealed record SearchResult(Guid DocumentId, string Title, double Score);
