using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ECM.Modules.Signature.Application.Requests;
using ECM.Modules.Signature.Domain.Requests;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace ECM.Modules.Signature.Api.Requests;

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

    private static async Task<Ok<IReadOnlyCollection<SignatureRequest>>> GetPending(SignatureApplicationService service, CancellationToken cancellationToken)
    {
        var pending = await service.GetPendingAsync(cancellationToken);
        return TypedResults.Ok(pending);
    }

    private static async Task<Results<Created<SignatureRequest>, ValidationProblem>> CreateSignature(CreateSignatureRequest request, SignatureApplicationService service, CancellationToken cancellationToken)
    {
        var command = new CreateSignatureRequestCommand(request.DocumentId, request.SignerEmail);
        var result = await service.CreateAsync(command, cancellationToken);

        if (result.IsFailure || result.Value is null)
        {
            var errors = new Dictionary<string, string[]>
            {
                ["signature"] = result.Errors.ToArray()
            };

            return TypedResults.ValidationProblem(errors);
        }

        return TypedResults.Created($"/api/ecm/signatures/{result.Value.Id}", result.Value);
    }
}
