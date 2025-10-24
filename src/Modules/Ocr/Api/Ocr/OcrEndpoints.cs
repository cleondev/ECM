using System.Collections.Generic;
using System.Text.Json;
using ECM.Ocr.Api.Ocr.Requests;
using ECM.Ocr.Application.Commands;
using ECM.Ocr.Application.Queries;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace ECM.Ocr.Api.Ocr;

public static class OcrEndpoints
{
    public static RouteGroupBuilder MapOcrEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/ocr");
        group.WithTags("OCR");
        group.WithGroupName(OcrSwagger.DocumentName);
        group.DisableAntiforgery();

        group.MapGet("/samples/{sampleId}/results", GetSampleResultAsync)
            .WithName("GetOcrSampleResult")
            .WithDescription("Retrieve the OCR result for the specified sample from Dot OCR.");

        group.MapGet("/samples/{sampleId}/boxes", ListBoxesAsync)
            .WithName("ListOcrBoxes")
            .WithDescription("List the recognized boxes for a sample.");

        group.MapGet("/samples/{sampleId}/boxes/{boxId}/results", GetBoxingResultAsync)
            .WithName("GetOcrBoxingResult")
            .WithDescription("Retrieve OCR result for a specific boxing within a sample.");

        group.MapPut("/samples/{sampleId}/boxes/{boxId}", SetBoxValueAsync)
            .WithName("SetOcrBoxValue")
            .WithDescription("Update the value for a specific OCR box.");

        return group;
    }

    private static async Task<Ok<JsonElement>> GetSampleResultAsync(
        string sampleId,
        GetOcrSampleResultQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetOcrSampleResultQuery(sampleId), cancellationToken)
            .ConfigureAwait(false);

        return TypedResults.Ok(result.Data);
    }

    private static async Task<Ok<JsonElement>> GetBoxingResultAsync(
        string sampleId,
        string boxId,
        GetOcrBoxingResultQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetOcrBoxingResultQuery(sampleId, boxId), cancellationToken)
            .ConfigureAwait(false);

        return TypedResults.Ok(result.Data);
    }

    private static async Task<Ok<JsonElement>> ListBoxesAsync(
        string sampleId,
        ListOcrBoxesQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ListOcrBoxesQuery(sampleId), cancellationToken)
            .ConfigureAwait(false);

        return TypedResults.Ok(result.Data);
    }

    private static async Task<Results<NoContent, ValidationProblem>> SetBoxValueAsync(
        string sampleId,
        string boxId,
        [FromBody] SetOcrBoxValueRequest request,
        SetOcrBoxValueCommandHandler handler,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Value))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["value"] = ["A non-empty value is required."]
            });
        }

        var command = new SetOcrBoxValueCommand(sampleId, boxId, request.Value);
        await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        return TypedResults.NoContent();
    }
}
