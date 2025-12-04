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
using ECM.Document.Application.Tags.Queries;
using ECM.Document.Application.Tags.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace ECM.Document.Api.Tags;

public static class TagEndpoints
{
    public static void MapTagEndpoints(this IEndpointRouteBuilder builder)
    {
        var tagGroup = builder.MapGroup("/api/tags");
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
        ITagLabelRepository tagLabelRepository,
        IUserLookupService userLookupService,
        CancellationToken cancellationToken
    )
    {
        var claimedUserId = await principal
            .GetUserObjectIdAsync(userLookupService, cancellationToken)
            .ConfigureAwait(false);
        var updatedBy = NormalizeGuid(request.UpdatedBy) ?? claimedUserId;

        var existingTag = await tagLabelRepository
            .GetByIdAsync(tagId, cancellationToken)
            .ConfigureAwait(false);

        if (existingTag is null)
        {
            return TypedResults.NotFound();
        }

        var command = new UpdateTagLabelCommand(
            tagId,
            existingTag.NamespaceId,
            request.ParentId,
            request.Name,
            request.SortOrder,
            TagEndpointMapping.NormalizeColor(request.Color),
            TagEndpointMapping.NormalizeIcon(request.IconKey, TagEndpointMapping.UserDefaultIconKey),
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

        return TypedResults.Ok(
            TagEndpointMapping.ToResponse(result.Value, TagEndpointMapping.UserDefaultIconKey)
        );
    }

    private static async Task<Ok<TagLabelResponse[]>> ListTagsAsync(
        ClaimsPrincipal principal,
        ListTagLabelsQueryHandler handler,
        IUserLookupService userLookupService,
        CancellationToken cancellationToken
    )
    {
        var ownerUserId = await principal
            .GetUserObjectIdAsync(userLookupService, cancellationToken)
            .ConfigureAwait(false);
        Guid? primaryGroupId = null;

        if (ownerUserId.HasValue)
        {
            primaryGroupId = await userLookupService
                .FindPrimaryGroupIdByUserIdAsync(ownerUserId.Value, cancellationToken)
                .ConfigureAwait(false);
        }

        var tagLabels = await handler
            .HandleAsync(ownerUserId, primaryGroupId, scope: null, includeAllNamespaces: false, cancellationToken)
            .ConfigureAwait(false);

        var response = tagLabels
            .Select(tag => TagEndpointMapping.ToResponse(tag, TagEndpointMapping.UserDefaultIconKey))
            .ToArray();

        return TypedResults.Ok(response);
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
            null,
            request.ParentId,
            request.Name,
            request.SortOrder,
            TagEndpointMapping.NormalizeColor(request.Color),
            TagEndpointMapping.NormalizeIcon(request.IconKey, TagEndpointMapping.UserDefaultIconKey),
            createdBy
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

        var response = TagEndpointMapping.ToResponse(result.Value, TagEndpointMapping.UserDefaultIconKey);

        return TypedResults.Created($"/api/tags/{response.Id}", response);
    }

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
