using System.Collections.Generic;

namespace ECM.Document.Api.Documents.Responses;

public sealed record DocumentListResponse(
    int Page,
    int PageSize,
    long TotalItems,
    int TotalPages,
    IReadOnlyCollection<DocumentResponse> Items);
