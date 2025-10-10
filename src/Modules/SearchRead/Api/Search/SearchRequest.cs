using Microsoft.AspNetCore.Mvc;

namespace ECM.SearchRead.Api.Search;

public sealed class SearchRequest
{
    [FromQuery(Name = "q")]
    public string Term { get; init; } = string.Empty;

    [FromQuery(Name = "department")]
    public string? Department { get; init; }

    [FromQuery(Name = "limit")]
    public int Limit { get; init; } = 20;
}
