using System.Collections.Generic;

namespace AppGateway.Contracts.Documents;

public sealed record DocumentListDto(
    int Page,
    int PageSize,
    long TotalItems,
    int TotalPages,
    IReadOnlyCollection<DocumentDto> Items)
{
    public static readonly DocumentListDto Empty = new(1, 0, 0, 0, []);
}
