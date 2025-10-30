using Microsoft.AspNetCore.Mvc;

namespace ECM.SearchRead.Api.Search;

public sealed class SuggestRequest
{
    [FromQuery(Name = "q")]
    public string Term { get; init; } = string.Empty;

    [FromQuery(Name = "limit")]
    public int Limit { get; init; } = 5;
}
