using System.Collections.Generic;
using System.Linq;
using ECM.Document.Application.Tags;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace ECM.Document.Api.Tags;

public static class TagEndpoints
{
    public static void MapTagEndpoints(this IEndpointRouteBuilder builder)
    {
        var tagGroup = builder.MapGroup("/api/ecm/tags");
        tagGroup.WithTags("Tags");

        tagGroup.MapPost("/", CreateTagAsync)
            .WithName("CreateTag")
            .WithDescription("Create a tag label within an existing namespace.");

        tagGroup.MapDelete("/{tagId:guid}", DeleteTagAsync)
            .WithName("DeleteTag")
            .WithDescription("Delete a tag label. Existing document assignments will be removed by cascade.");

        var documentTagGroup = builder.MapGroup("/api/ecm/documents/{documentId:guid}/tags");
        documentTagGroup.WithTags("Document Tags");

        documentTagGroup.MapPost("/", AssignTagAsync)
            .WithName("AssignDocumentTag")
            .WithDescription("Assign a tag label to a document.");

        documentTagGroup.MapDelete("/{tagId:guid}", RemoveTagAsync)
            .WithName("RemoveDocumentTag")
            .WithDescription("Remove a tag label assignment from a document.");
    }

    private static async Task<Results<Created<TagLabelResponse>, ValidationProblem>> CreateTagAsync(
        CreateTagRequest request,
        TagApplicationService service,
        CancellationToken cancellationToken)
    {
        var command = new CreateTagLabelCommand(request.NamespaceSlug, request.Slug, request.Path, request.CreatedBy);
        var result = await service.CreateTagAsync(command, cancellationToken);

        if (result.IsFailure || result.Value is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["tag"] = [.. result.Errors]
            });
        }

        var response = new TagLabelResponse(
            result.Value.Id,
            result.Value.NamespaceSlug,
            result.Value.Slug,
            result.Value.Path,
            result.Value.IsActive,
            result.Value.CreatedBy,
            result.Value.CreatedAtUtc);

        return TypedResults.Created($"/api/ecm/tags/{response.Id}", response);
    }

    private static async Task<Results<NoContent, ValidationProblem>> DeleteTagAsync(
        Guid tagId,
        TagApplicationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.DeleteTagAsync(tagId, cancellationToken);
        if (result.IsFailure)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["tag"] = [.. result.Errors]
            });
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, ValidationProblem>> AssignTagAsync(
        Guid documentId,
        AssignTagRequest request,
        TagApplicationService service,
        CancellationToken cancellationToken)
    {
        var command = new AssignTagToDocumentCommand(documentId, request.TagId, request.AppliedBy);
        var result = await service.AssignTagAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["tag"] = [.. result.Errors]
            });
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, ValidationProblem>> RemoveTagAsync(
        Guid documentId,
        Guid tagId,
        TagApplicationService service,
        CancellationToken cancellationToken)
    {
        var command = new RemoveTagFromDocumentCommand(documentId, tagId);
        var result = await service.RemoveTagAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["tag"] = [.. result.Errors]
            });
        }

        return TypedResults.NoContent();
    }
}
