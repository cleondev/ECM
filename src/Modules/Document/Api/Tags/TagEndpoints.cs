using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ECM.Abstractions.Users;
using ECM.Document.Api.Documents.Extensions;
using ECM.Document.Api.Tags.Requests;
using ECM.Document.Api.Tags.Responses;
using ECM.Document.Application.Tags.Commands;
using ECM.Document.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECM.Document.Api.Tags;

public static class TagEndpoints
{
    public static void MapTagEndpoints(this IEndpointRouteBuilder builder)
    {
        var tagGroup = builder.MapGroup("/api/ecm/tags");
        tagGroup.WithTags("Tags");
        tagGroup.WithGroupName(DocumentSwagger.DocumentName);

        tagGroup
            .MapGet("/", ListTagsAsync)
            .WithName("ListTags")
            .WithDescription("Retrieve all tag labels grouped by namespace.");

        tagGroup
            .MapPost("/", CreateTagAsync)
            .WithName("CreateTag")
            .WithDescription("Create a tag label within an existing namespace.");

        tagGroup
            .MapPut("/{tagId:guid}", UpdateTagAsync)
            .WithName("UpdateTag")
            .WithDescription("Update an existing tag label.");

        tagGroup
            .MapDelete("/{tagId:guid}", DeleteTagAsync)
            .WithName("DeleteTag")
            .WithDescription(
                "Delete a tag label. Existing document assignments will be removed by cascade."
            );

        var documentTagGroup = builder.MapGroup("/api/ecm/documents/{documentId:guid}/tags");
        documentTagGroup.WithTags("Document Tags");
        documentTagGroup.WithGroupName(DocumentSwagger.DocumentName);

        documentTagGroup
            .MapPost("/", AssignTagAsync)
            .WithName("AssignDocumentTag")
            .WithDescription("Assign a tag label to a document.");

        documentTagGroup
            .MapDelete("/{tagId:guid}", RemoveTagAsync)
            .WithName("RemoveDocumentTag")
            .WithDescription("Remove a tag label assignment from a document.");
    }

    private static async Task<
        Results<Ok<TagLabelResponse>, ValidationProblem, NotFound>
    > UpdateTagAsync(
        Guid tagId,
        ClaimsPrincipal principal,
        UpdateTagRequest request,
        UpdateTagLabelCommandHandler handler,
        IUserLookupService userLookupService,
        CancellationToken cancellationToken
    )
    {
        var claimedUserId = await principal
            .GetUserObjectIdAsync(userLookupService, cancellationToken)
            .ConfigureAwait(false);
        var updatedBy = NormalizeGuid(request.UpdatedBy) ?? claimedUserId;

        var command = new UpdateTagLabelCommand(
            tagId,
            request.NamespaceId,
            request.ParentId,
            request.Name,
            request.SortOrder,
            request.Color,
            request.IconKey,
            request.IsActive,
            updatedBy
        );
        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            if (UpdateTagLabelCommandHandler.IsNotFound(result))
            {
                return TypedResults.NotFound();
            }

            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { ["tag"] = [.. result.Errors] }
            );
        }

        if (result.Value is null)
        {
            return TypedResults.NotFound();
        }

        var response = new TagLabelResponse(
            result.Value.Id,
            result.Value.NamespaceId,
            result.Value.NamespaceDisplayName,
            result.Value.ParentId,
            result.Value.Name,
            result.Value.PathIds,
            result.Value.SortOrder,
            result.Value.Color,
            result.Value.IconKey,
            result.Value.IsActive,
            result.Value.IsSystem,
            result.Value.CreatedBy,
            result.Value.CreatedAtUtc
        );

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<TagLabelResponse[]>> ListTagsAsync(
        DocumentDbContext context,
        CancellationToken cancellationToken
    )
    {
        var tagRecords = await (
            from label in context.TagLabels.AsNoTracking()
            join ns in context.TagNamespaces.AsNoTracking()
                on label.NamespaceId equals ns.Id into namespaceGroup
            from ns in namespaceGroup.DefaultIfEmpty()
            orderby label.NamespaceId,
                label.ParentId.HasValue ? 1 : 0,
                label.SortOrder,
                label.Name
            select new
            {
                label.Id,
                label.NamespaceId,
                NamespaceDisplayName = ns != null ? ns.DisplayName : null,
                NamespaceScope = ns != null ? ns.Scope : null,
                label.ParentId,
                label.Name,
                label.PathIds,
                label.SortOrder,
                label.Color,
                label.IconKey,
                label.IsActive,
                label.IsSystem,
                label.CreatedBy,
                label.CreatedAtUtc,
            }
        ).ToArrayAsync(cancellationToken);

        var tags = tagRecords
            .Select(record => new TagLabelResponse(
                record.Id,
                record.NamespaceId,
                NormalizeNamespaceDisplayName(record.NamespaceDisplayName, record.NamespaceScope),
                record.ParentId,
                record.Name,
                record.PathIds,
                record.SortOrder,
                record.Color,
                record.IconKey,
                record.IsActive,
                record.IsSystem,
                record.CreatedBy,
                record.CreatedAtUtc
            ))
            .ToArray();

        return TypedResults.Ok(tags);
    }

    private static async Task<Results<Created<TagLabelResponse>, ValidationProblem>> CreateTagAsync(
        ClaimsPrincipal principal,
        CreateTagRequest request,
        CreateTagLabelCommandHandler handler,
        IUserLookupService userLookupService,
        ILogger<TagEndpointsLoggingCategory> logger,
        CancellationToken cancellationToken
    )
    {
        var claimedUserId = await principal
            .GetUserObjectIdAsync(userLookupService, cancellationToken)
            .ConfigureAwait(false);
        var createdBy = NormalizeGuid(request.CreatedBy) ?? claimedUserId;

        if (createdBy is null)
        {
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["createdBy"] =
                    [
                        "The creator could not be determined from the request or user context.",
                    ],
                }
            );
        }

        var command = new CreateTagLabelCommand(
            request.NamespaceId,
            request.ParentId,
            request.Name,
            request.SortOrder,
            request.Color,
            request.IconKey,
            createdBy,
            request.IsSystem
        );
        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure || result.Value is null)
        {
            if (result.Errors.Count > 0)
            {
                logger.LogWarning(
                    "Failed to create tag label for namespace {NamespaceId} and name {Name}. Errors: {Errors}",
                    request.NamespaceId,
                    request.Name,
                    string.Join(", ", result.Errors)
                );
            }

            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { ["tag"] = [.. result.Errors] }
            );
        }

        var response = new TagLabelResponse(
            result.Value.Id,
            result.Value.NamespaceId,
            result.Value.NamespaceDisplayName,
            result.Value.ParentId,
            result.Value.Name,
            result.Value.PathIds,
            result.Value.SortOrder,
            result.Value.Color,
            result.Value.IconKey,
            result.Value.IsActive,
            result.Value.IsSystem,
            result.Value.CreatedBy,
            result.Value.CreatedAtUtc
        );

        return TypedResults.Created($"/api/ecm/tags/{response.Id}", response);
    }

    private static string? NormalizeNamespaceDisplayName(string? displayName, string? fallback)
        => string.IsNullOrWhiteSpace(displayName) ? fallback : displayName.Trim();

    private static Guid? NormalizeGuid(Guid? value)
        => value.HasValue && value.Value != Guid.Empty ? value : null;

    private static async Task<Results<NoContent, ValidationProblem>> DeleteTagAsync(
        Guid tagId,
        DeleteTagLabelCommandHandler handler,
        CancellationToken cancellationToken
    )
    {
        var command = new DeleteTagLabelCommand(tagId);
        var result = await handler.HandleAsync(command, cancellationToken);
        if (result.IsFailure)
        {
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { ["tag"] = [.. result.Errors] }
            );
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, ValidationProblem>> AssignTagAsync(
        Guid documentId,
        AssignTagRequest request,
        AssignTagToDocumentCommandHandler handler,
        CancellationToken cancellationToken
    )
    {
        var command = new AssignTagToDocumentCommand(documentId, request.TagId, request.AppliedBy);
        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { ["tag"] = [.. result.Errors] }
            );
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, ValidationProblem>> RemoveTagAsync(
        Guid documentId,
        Guid tagId,
        RemoveTagFromDocumentCommandHandler handler,
        CancellationToken cancellationToken
    )
    {
        var command = new RemoveTagFromDocumentCommand(documentId, tagId);
        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { ["tag"] = [.. result.Errors] }
            );
        }

        return TypedResults.NoContent();
    }
}

internal sealed class TagEndpointsLoggingCategory;
