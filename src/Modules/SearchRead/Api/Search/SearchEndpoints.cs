using System.Collections.Generic;
using System.Threading;
using ECM.Modules.SearchRead.Application.Search;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace ECM.Modules.SearchRead.Api.Search;

public static class SearchEndpoints
{
    public static RouteGroupBuilder MapSearchEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/ecm/search");
        group.WithTags("Search");

        group.MapGet("/", SearchAsync)
             .WithName("SearchDocuments");

        return group;
    }

    private static async Task<Ok<IReadOnlyCollection<SearchResult>>> SearchAsync([AsParameters] SearchRequest request, SearchApplicationService service, CancellationToken cancellationToken)
    {
        var query = new SearchQuery(request.Term, request.Department, request.Limit);
        var results = await service.SearchAsync(query, cancellationToken);
        return TypedResults.Ok(results);
    }
}
