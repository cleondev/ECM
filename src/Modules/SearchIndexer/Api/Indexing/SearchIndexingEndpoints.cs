using System;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using ECM.SearchIndexer.Application.Indexing;
using ECM.SearchIndexer.Application.Indexing.Abstractions;
using ECM.SearchIndexer.Domain.Indexing.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace ECM.SearchIndexer.Api.Indexing;

internal static class SearchIndexingEndpoints
{
    public static IEndpointRouteBuilder MapSearchIndexingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/search-indexer")
            .WithTags("SearchIndexer")
            .WithGroupName("SearchIndexer")
            .WithMetadata(new ProducesAttribute(MediaTypeNames.Application.Json));

        group.MapPost("/documents", EnqueueDocumentIndexingAsync)
            .Produces<EnqueueDocumentIndexingResult>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithName("EnqueueDocumentIndexing");

        group.MapGet("/documents/{documentId:guid}", GetDocumentIndexAsync)
            .Produces<SearchIndexDocumentResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("GetDocumentIndex");

        group.MapGet("/documents", ListDocumentIndexesAsync)
            .Produces<SearchIndexDocumentResponse[]>(StatusCodes.Status200OK)
            .WithName("ListDocumentIndexes");

        return endpoints;
    }

    private static async Task<IResult> EnqueueDocumentIndexingAsync(
        EnqueueDocumentIndexingRequest request,
        EnqueueDocumentIndexingHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await handler.HandleAsync(request.ToCommand(), cancellationToken).ConfigureAwait(false);
            return Results.Accepted($"/search-indexer/jobs/{result.JobId}", result);
        }
        catch (InvalidSearchDocumentException exception)
        {
            return Results.Problem(exception.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> GetDocumentIndexAsync(
        Guid documentId,
        ISearchIndexReader reader,
        CancellationToken cancellationToken)
    {
        var record = await reader.FindByDocumentIdAsync(documentId, cancellationToken).ConfigureAwait(false);
        if (record is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(SearchIndexDocumentResponse.FromRecord(record));
    }

    private static async Task<IResult> ListDocumentIndexesAsync(
        ISearchIndexReader reader,
        CancellationToken cancellationToken)
    {
        var records = await reader.ListAsync(cancellationToken).ConfigureAwait(false);
        var payload = records.Select(SearchIndexDocumentResponse.FromRecord).ToArray();
        return Results.Ok(payload);
    }
}
