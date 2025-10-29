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

        group.MapGet("/suggest", SuggestAsync)
             .WithName("SuggestDocuments");

        group.MapGet("/facets", FacetsAsync)
             .WithName("SearchFacets");

        return group;
    }

    private static async Task<Ok<IReadOnlyCollection<SearchResult>>> SearchAsync([AsParameters] SearchRequest request, SearchQueryHandler handler, CancellationToken cancellationToken)
    {
        var query = new SearchQuery(request.Term, request.GroupId, request.Limit);
        var results = await handler.HandleAsync(query, cancellationToken);
        return TypedResults.Ok(results);
    }

    private static async Task<Ok<IReadOnlyCollection<string>>> SuggestAsync([AsParameters] SuggestRequest request, SuggestQueryHandler handler, CancellationToken cancellationToken)
    {
        var query = new SuggestQuery(request.Term, request.Limit);
        var suggestions = await handler.HandleAsync(query, cancellationToken);
        return TypedResults.Ok(suggestions);
    }

    private static async Task<Ok<SearchFacetsResult>> FacetsAsync([AsParameters] SearchFacetsRequest request, SearchFacetsQueryHandler handler, CancellationToken cancellationToken)
    {
        var query = new SearchFacetsQuery(request.Term, request.GroupId);
        var facets = await handler.HandleAsync(query, cancellationToken);
        return TypedResults.Ok(facets);
    }
}
