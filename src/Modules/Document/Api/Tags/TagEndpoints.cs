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
        MapTagRoutes(builder.MapGroup("/api/ecm/tags"));
        MapTagRoutes(builder.MapGroup("/api/tags").ExcludeFromDescription());

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

    private static void MapTagRoutes(RouteGroupBuilder tagGroup)
    {
        tagGroup.WithTags("Tags");
        tagGroup.WithGroupName(DocumentSwagger.DocumentName);

        tagGroup
            .MapGet("/", ListTagsAsync)
            .WithName("ListTags")
            .WithDescription("Retrieve all tag labels grouped by namespace.");

        tagGroup
            .MapGet("/namespaces", ListNamespacesAsync)
            .WithName("ListTagNamespaces")
            .WithDescription("Retrieve tag namespaces available to the caller or all namespaces when requested.");

        tagGroup
            .MapPost("/", CreateTagAsync)
            .WithName("CreateTag")
            .WithDescription("Create a tag label within an existing namespace.");

        tagGroup
            .MapPost("/namespaces", CreateNamespaceAsync)
            .WithName("CreateTagNamespace")
            .WithDescription("Create a new tag namespace.");

        tagGroup
            .MapPut("/{tagId:guid}", UpdateTagAsync)
            .WithName("UpdateTag")
            .WithDescription("Update an existing tag label.");

        tagGroup
            .MapPut("/namespaces/{namespaceId:guid}", UpdateNamespaceAsync)
            .WithName("UpdateTagNamespace")
            .WithDescription("Update a tag namespace.");

        tagGroup
            .MapDelete("/{tagId:guid}", DeleteTagAsync)
            .WithName("DeleteTag")
            .WithDescription(
                "Delete a tag label. Existing document assignments will be removed by cascade."
            );

        tagGroup
            .MapDelete("/namespaces/{namespaceId:guid}", DeleteNamespaceAsync)
            .WithName("DeleteTagNamespace")
            .WithDescription("Delete an empty tag namespace.");
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
            result.Value.NamespaceScope,
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

    private static async Task<Results<NoContent, ValidationProblem, NotFound>> UpdateNamespaceAsync(
        Guid namespaceId,
        ClaimsPrincipal principal,
        UpdateTagNamespaceRequest request,
        UpdateTagNamespaceCommandHandler handler,
        IUserLookupService userLookupService,
        CancellationToken cancellationToken)
    {
        var claimedUserId = await principal
            .GetUserObjectIdAsync(userLookupService, cancellationToken)
            .ConfigureAwait(false);
        var updatedBy = NormalizeGuid(request.UpdatedBy) ?? claimedUserId;

        var command = new UpdateTagNamespaceCommand(namespaceId, request.DisplayName, updatedBy);

        var result = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            if (result.Errors.Contains("Tag namespace was not found."))
            {
                return TypedResults.NotFound();
            }

            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["namespace"] = [.. result.Errors] });
        }

        return TypedResults.NoContent();
    }

    private static async Task<Ok<TagLabelResponse[]>> ListTagsAsync(
        ClaimsPrincipal principal,
        ListTagLabelsQueryHandler handler,
        IUserLookupService userLookupService,
        [FromQuery(Name = "scope")] string? scope,
        [FromQuery(Name = "includeAll")] bool? includeAll,
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
            .HandleAsync(ownerUserId, primaryGroupId, scope, includeAll ?? false, cancellationToken)
            .ConfigureAwait(false);

        var response = tagLabels
            .Select(tag => new TagLabelResponse(
                tag.Id,
                tag.NamespaceId,
                tag.NamespaceScope,
                tag.NamespaceDisplayName,
                tag.ParentId,
                tag.Name,
                tag.PathIds,
                tag.SortOrder,
                tag.Color,
                tag.IconKey,
                tag.IsActive,
                tag.IsSystem,
                tag.CreatedBy,
                tag.CreatedAtUtc
            ))
            .ToArray();

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<TagNamespaceResponse[]>> ListNamespacesAsync(
        ClaimsPrincipal principal,
        ListTagNamespacesQueryHandler handler,
        IUserLookupService userLookupService,
        [FromQuery(Name = "scope")] string? scope,
        [FromQuery(Name = "includeAll")] bool? includeAll,
        CancellationToken cancellationToken)
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

        var namespaces = await handler
            .HandleAsync(ownerUserId, primaryGroupId, scope, includeAll ?? false, cancellationToken)
            .ConfigureAwait(false);

        var response = namespaces
            .Select(ns => new TagNamespaceResponse(
                ns.Id,
                ns.Scope,
                ns.OwnerUserId,
                ns.OwnerGroupId,
                ns.DisplayName,
                ns.IsSystem,
                ns.CreatedAtUtc))
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
            result.Value.NamespaceScope,
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

    private static async Task<Results<Created<TagNamespaceResponse>, ValidationProblem>> CreateNamespaceAsync(
        ClaimsPrincipal principal,
        CreateTagNamespaceRequest request,
        CreateTagNamespaceCommandHandler handler,
        IUserLookupService userLookupService,
        CancellationToken cancellationToken)
    {
        var claimedUserId = await principal
            .GetUserObjectIdAsync(userLookupService, cancellationToken)
            .ConfigureAwait(false);
        var createdBy = NormalizeGuid(request.CreatedBy) ?? claimedUserId;

        var command = new CreateTagNamespaceCommand(
            request.Scope,
            request.OwnerUserId,
            request.OwnerGroupId,
            request.DisplayName,
            createdBy);

        var result = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["namespace"] = [.. result.Errors] });
        }

        var response = new TagNamespaceResponse(
            result.Value.Id,
            result.Value.Scope,
            result.Value.OwnerUserId,
            result.Value.OwnerGroupId,
            result.Value.DisplayName,
            result.Value.IsSystem,
            result.Value.CreatedAtUtc);

        return TypedResults.Created($"/api/ecm/tags/namespaces/{response.Id}", response);
    }

    private static async Task<Results<NoContent, ValidationProblem>> DeleteNamespaceAsync(
        Guid namespaceId,
        DeleteTagNamespaceCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler
            .HandleAsync(new DeleteTagNamespaceCommand(namespaceId), cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["namespace"] = [.. result.Errors] });
        }

        return TypedResults.NoContent();
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
