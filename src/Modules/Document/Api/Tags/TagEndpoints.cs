using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ECM.Document.Api;
using ECM.Document.Application.Tags.Commands;
using ECM.Document.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace ECM.Document.Api.Tags;

public static class TagEndpoints
{
    public static void MapTagEndpoints(this IEndpointRouteBuilder builder)
    {
        var tagGroup = builder.MapGroup("/api/ecm/tags");
        tagGroup.WithTags("Tags");
        tagGroup.WithGroupName(DocumentSwagger.DocumentName);

        tagGroup.MapGet("/", ListTagsAsync)
            .WithName("ListTags")
            .WithDescription("Retrieve all tag labels grouped by namespace.");

        tagGroup.MapPost("/", CreateTagAsync)
            .WithName("CreateTag")
            .WithDescription("Create a tag label within an existing namespace.");

        tagGroup.MapPut("/{tagId:guid}", UpdateTagAsync)
            .WithName("UpdateTag")
            .WithDescription("Update an existing tag label.");

        tagGroup.MapDelete("/{tagId:guid}", DeleteTagAsync)
            .WithName("DeleteTag")
            .WithDescription("Delete a tag label. Existing document assignments will be removed by cascade.");

        var documentTagGroup = builder.MapGroup("/api/ecm/documents/{documentId:guid}/tags");
        documentTagGroup.WithTags("Document Tags");
        documentTagGroup.WithGroupName(DocumentSwagger.DocumentName);

        documentTagGroup.MapPost("/", AssignTagAsync)
            .WithName("AssignDocumentTag")
            .WithDescription("Assign a tag label to a document.");

        documentTagGroup.MapDelete("/{tagId:guid}", RemoveTagAsync)
            .WithName("RemoveDocumentTag")
            .WithDescription("Remove a tag label assignment from a document.");
    }

    private static async Task<Results<Ok<TagLabelResponse>, ValidationProblem, NotFound>> UpdateTagAsync(
        Guid tagId,
        UpdateTagRequest request,
        UpdateTagLabelCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTagLabelCommand(tagId, request.NamespaceSlug, request.Slug, request.Path, request.UpdatedBy);
        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            if (UpdateTagLabelCommandHandler.IsNotFound(result))
            {
                return TypedResults.NotFound();
            }

            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["tag"] = [.. result.Errors]
            });
        }

        if (result.Value is null)
        {
            return TypedResults.NotFound();
        }

        var response = new TagLabelResponse(
            result.Value.Id,
            result.Value.NamespaceSlug,
            result.Value.Slug,
            result.Value.Path,
            result.Value.IsActive,
            result.Value.CreatedBy,
            result.Value.CreatedAtUtc);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<TagLabelResponse[]>> ListTagsAsync(
        DocumentDbContext context,
        CancellationToken cancellationToken)
    {
        var tags = await context.TagLabels
            .AsNoTracking()
            .OrderBy(label => label.NamespaceSlug)
            .ThenBy(label => label.Path)
            .Select(label => new TagLabelResponse(
                label.Id,
                label.NamespaceSlug,
                label.Slug,
                label.Path,
                label.IsActive,
                label.CreatedBy,
                label.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);

        return TypedResults.Ok(tags);
    }

    private static async Task<Results<Created<TagLabelResponse>, ValidationProblem>> CreateTagAsync(
        CreateTagRequest request,
        CreateTagLabelCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateTagLabelCommand(request.NamespaceSlug, request.Slug, request.Path, request.CreatedBy);
        var result = await handler.HandleAsync(command, cancellationToken);

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
        DeleteTagLabelCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new DeleteTagLabelCommand(tagId);
        var result = await handler.HandleAsync(command, cancellationToken);
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
        AssignTagToDocumentCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new AssignTagToDocumentCommand(documentId, request.TagId, request.AppliedBy);
        var result = await handler.HandleAsync(command, cancellationToken);

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
        RemoveTagFromDocumentCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new RemoveTagFromDocumentCommand(documentId, tagId);
        var result = await handler.HandleAsync(command, cancellationToken);

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
