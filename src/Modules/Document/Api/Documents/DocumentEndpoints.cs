using System.Collections.Generic;
using System.Linq;
using ECM.Modules.Document.Application.Documents;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace ECM.Modules.Document.Api.Documents;

public static class DocumentEndpoints
{
    public static RouteGroupBuilder MapDocumentEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/ecm");
        group.WithTags("ECM");

        group.MapGet("/", () => Results.Ok(new { message = "ECM API ready" }))
             .WithName("GetEcmStatus")
             .WithDescription("Return a readiness payload for the ECM edge API.");

        group.MapPost("/documents", CreateDocumentAsync)
             .WithName("CreateDocument")
             .WithDescription("Create a document using the application service (Clean Architecture).");

        return group;
    }

    private static async Task<Results<Created<DocumentResponse>, ValidationProblem>> CreateDocumentAsync(
        CreateDocumentRequest request,
        DocumentApplicationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(new CreateDocumentCommand(request.Title), cancellationToken);

        if (result.IsFailure || result.Value is null)
        {
            var errors = new Dictionary<string, string[]>
            {
                [nameof(request.Title).ToLowerInvariant()] = result.Errors.ToArray()
            };

            return TypedResults.ValidationProblem(errors);
        }

        var response = new DocumentResponse(result.Value.Id, result.Value.Title, result.Value.CreatedAtUtc);
        return TypedResults.Created($"/api/ecm/documents/{response.Id}", response);
    }
}
