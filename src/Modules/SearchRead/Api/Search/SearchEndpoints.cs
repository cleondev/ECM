using System.Collections.Generic;
using System.Threading;
using ECM.SearchRead.Api;
using ECM.SearchRead.Application.Search;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace ECM.SearchRead.Api.Search;

public static class SearchEndpoints
{
    public static RouteGroupBuilder MapSearchEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/ecm/search");
        group.WithTags("Search");
        group.WithGroupName(SearchReadSwagger.DocumentName);

        group.MapGet("/", SearchAsync)
             .WithName("SearchDocuments");

        return group;
    }

    private static async Task<Ok<IReadOnlyCollection<SearchResult>>> SearchAsync([AsParameters] SearchRequest request, SearchQueryHandler handler, CancellationToken cancellationToken)
    {
        var query = new SearchQuery(request.Term, request.Department, request.Limit);
        var results = await handler.HandleAsync(query, cancellationToken);
        return TypedResults.Ok(results);
    }
}
