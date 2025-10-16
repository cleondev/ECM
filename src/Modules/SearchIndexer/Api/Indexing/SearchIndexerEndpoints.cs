using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using ECM.SearchIndexer.Application.Indexing;
using ECM.SearchIndexer.Domain.Indexing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace ECM.SearchIndexer.Api.Indexing;

public static class SearchIndexerEndpoints
{
    public static RouteGroupBuilder MapSearchIndexerEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/ecm/search-indexer");
        group.WithTags("SearchIndexer");
        group.WithGroupName(SearchIndexerSwagger.DocumentName);

        group.MapPost("/documents/{documentId:guid}", EnqueueDocumentIndexing)
             .WithName("EnqueueDocumentIndexing");

        return group;
    }

    private static async Task<Results<Accepted<EnqueueDocumentIndexingResult>, ValidationProblem>> EnqueueDocumentIndexing(
        Guid documentId,
        EnqueueIndexRequest request,
        EnqueueDocumentIndexingHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new EnqueueDocumentIndexingCommand(
            documentId,
            request.Title,
            request.Summary,
            request.Content,
            request.Metadata,
            request.Tags,
            request.IndexingType);

        var result = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);
        return TypedResults.Accepted($"/api/ecm/search-indexer/jobs/{result.JobId}", result);
    }
}

public sealed class EnqueueIndexRequest
{
    [Required]
    public string Title { get; init; } = string.Empty;

    public string? Summary { get; init; }

    public string? Content { get; init; }

    public IDictionary<string, string>? Metadata { get; init; }

    public IReadOnlyCollection<string>? Tags { get; init; }

    public SearchIndexingType IndexingType { get; init; } = SearchIndexingType.Basic;
}
