using System.Collections.Generic;
using System.Linq;
using ECM.Document.Application.Documents;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace ECM.Document.Api.Documents;

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
        var result = await service.CreateAsync(
            new CreateDocumentCommand(
                request.Title,
                request.DocType,
                request.Status,
                request.OwnerId,
                request.CreatedBy,
                request.Department,
                request.Sensitivity,
                request.DocumentTypeId),
            cancellationToken);

        if (result.IsFailure || result.Value is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["document"] = result.Errors.ToArray()
            });
        }

        var response = new DocumentResponse(
            result.Value.Id,
            result.Value.Title,
            result.Value.DocType,
            result.Value.Status,
            result.Value.Sensitivity,
            result.Value.OwnerId,
            result.Value.CreatedBy,
            result.Value.Department,
            result.Value.CreatedAtUtc,
            result.Value.UpdatedAtUtc,
            result.Value.DocumentTypeId);

        return TypedResults.Created($"/api/ecm/documents/{response.Id}", response);
    }
}
