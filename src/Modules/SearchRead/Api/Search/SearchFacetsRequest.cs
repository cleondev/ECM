using Microsoft.AspNetCore.Mvc;

namespace ECM.SearchRead.Api.Search;

public sealed class SearchFacetsRequest
{
    [FromQuery(Name = "q")]
    public string? Term { get; init; }

    [FromQuery(Name = "groupId")]
    public string? GroupId { get; init; }
}
