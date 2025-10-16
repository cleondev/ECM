using System.Collections.Generic;

namespace ECM.Document.Api.Documents;

public sealed record DocumentHistoryListResponse(
    int Page,
    int PageSize,
    long TotalItems,
    int TotalPages,
    IReadOnlyCollection<DocumentHistoryEntryResponse> Items);
