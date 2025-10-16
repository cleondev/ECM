using System;
using System.Collections.Generic;
using System.Threading;
using ECM.Signature.Api;
using ECM.Signature.Application.Requests.Commands;
using ECM.Signature.Application.Requests.Queries;
using ECM.Signature.Domain.Requests;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace ECM.Signature.Api.Requests;

public static class SignatureEndpoints
{
    public static RouteGroupBuilder MapSignatureEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/ecm/signatures");
        group.WithTags("Signature");
        group.WithGroupName(SignatureSwagger.DocumentName);

        group.MapGet("/", GetSignatures)
             .WithName("GetSignatureRequests");

        group.MapPost("/", CreateSignature)
             .WithName("CreateSignatureRequest");

        group.MapGet("/{id:guid}", GetSignatureById)
             .WithName("GetSignatureRequestById");

        group.MapPost("/{id:guid}/cancel", CancelSignature)
             .WithName("CancelSignatureRequest");

        return group;
    }

    private static async Task<Ok<IReadOnlyCollection<SignatureRequest>>> GetSignatures(
        [AsParameters] SignatureListRequest request,
        GetSignatureRequestsQueryHandler handler,
        CancellationToken cancellationToken)
    {
        SignatureStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<SignatureStatus>(request.Status, true, out var parsed))
        {
            status = parsed;
        }

        var query = new GetSignatureRequestsQuery(status);
        var requests = await handler.HandleAsync(query, cancellationToken);
        return TypedResults.Ok(requests);
    }

    private static async Task<Results<Created<SignatureRequest>, ValidationProblem>> CreateSignature(
        CreateSignatureRequest request,
        CreateSignatureRequestCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateSignatureRequestCommand(request.DocumentId, request.SignerEmail);
        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure || result.Value is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["signature"] = [.. result.Errors]
            });
        }

        return TypedResults.Created($"/api/ecm/signatures/{result.Value.Id}", result.Value);
    }

    private static async Task<Results<Ok<SignatureRequest>, NotFound>> GetSignatureById(
        Guid id,
        GetSignatureRequestByIdQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var request = await handler.HandleAsync(new GetSignatureRequestByIdQuery(id), cancellationToken);
        return request is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(request);
    }

    private static async Task<Results<NoContent, ValidationProblem>> CancelSignature(
        Guid id,
        CancelSignatureRequestCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new CancelSignatureRequestCommand(id);
        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["signature"] = [result.Error]
            });
        }

        return TypedResults.NoContent();
    }
}
