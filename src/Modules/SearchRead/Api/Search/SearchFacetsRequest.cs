using Microsoft.AspNetCore.Mvc;

namespace ECM.SearchRead.Api.Search;

public sealed class SearchFacetsRequest
{
    [FromQuery(Name = "q")]
    public string? Term { get; init; }

    [FromQuery(Name = "dept")]
    public string? Department { get; init; }
}
