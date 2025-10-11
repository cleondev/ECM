using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ECM.Signature.Application.Requests.Commands;
using ECM.Signature.Application.Requests.Queries;
using ECM.Signature.Domain.Requests;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace ECM.Signature.Api.Requests;

public static class SignatureEndpoints
{
    public static RouteGroupBuilder MapSignatureEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/ecm/signatures");
        group.WithTags("Signature");

        group.MapGet("/", GetPending)
             .WithName("GetPendingSignatures");

        group.MapPost("/", CreateSignature)
             .WithName("CreateSignatureRequest");

        return group;
    }

    private static async Task<Ok<IReadOnlyCollection<SignatureRequest>>> GetPending(GetPendingSignatureRequestsQueryHandler handler, CancellationToken cancellationToken)
    {
        var pending = await handler.HandleAsync(new GetPendingSignatureRequestsQuery(), cancellationToken);
        return TypedResults.Ok(pending);
    }

    private static async Task<Results<Created<SignatureRequest>, ValidationProblem>> CreateSignature(CreateSignatureRequest request, CreateSignatureRequestCommandHandler handler, CancellationToken cancellationToken)
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
}
