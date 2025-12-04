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

public static class TagManagementEndpoints
{
    public static void MapTagManagementEndpoints(this IEndpointRouteBuilder builder)
    {
        var managementGroup = builder.MapGroup("/api/tag-management");
        managementGroup.WithTags("Tag Management");
        managementGroup.WithGroupName(DocumentSwagger.DocumentName);

        managementGroup
            .MapGet("/tags", ListTagsAsync)
            .WithName("ListAllTags")
            .WithDescription("Retrieve all tag labels across namespaces.");

        managementGroup
            .MapGet("/namespaces", ListNamespacesAsync)
            .WithName("ListAllTagNamespaces")
            .WithDescription("Retrieve every tag namespace.");

        managementGroup
            .MapPost("/tags", CreateTagAsync)
            .WithName("CreateManagedTag")
            .WithDescription("Create a tag label within a specific namespace.");

        managementGroup
            .MapPost("/namespaces", CreateNamespaceAsync)
            .WithName("CreateManagedNamespace")
            .WithDescription("Create a tag namespace with explicit ownership.");

        managementGroup
            .MapPut("/tags/{tagId:guid}", UpdateTagAsync)
            .WithName("UpdateManagedTag")
            .WithDescription("Update an existing tag label in a specific namespace.");

        managementGroup
            .MapPut("/namespaces/{namespaceId:guid}", UpdateNamespaceAsync)
            .WithName("UpdateManagedNamespace")
            .WithDescription("Update a tag namespace.");

        managementGroup
            .MapDelete("/tags/{tagId:guid}", DeleteTagAsync)
            .WithName("DeleteManagedTag")
            .WithDescription("Delete a tag label regardless of namespace scope.");

        managementGroup
            .MapDelete("/namespaces/{namespaceId:guid}", DeleteNamespaceAsync)
            .WithName("DeleteManagedNamespace")
            .WithDescription("Delete a tag namespace.");
    }

    private static async Task<Ok<TagLabelResponse[]>> ListTagsAsync(
        ListTagLabelsQueryHandler handler,
        [FromQuery(Name = "scope")] string? scope,
        CancellationToken cancellationToken)
    {
        var tagLabels = await handler
            .HandleAsync(ownerUserId: null, primaryGroupId: null, scope, includeAllNamespaces: true, cancellationToken)
            .ConfigureAwait(false);

        var response = tagLabels
            .Select(tag => TagEndpointMapping.ToResponse(tag, TagEndpointMapping.ManagementDefaultIconKey))
            .ToArray();

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<TagNamespaceResponse[]>> ListNamespacesAsync(
        ListTagNamespacesQueryHandler handler,
        [FromQuery(Name = "scope")] string? scope,
        CancellationToken cancellationToken)
    {
        var namespaces = await handler
            .HandleAsync(ownerUserId: null, primaryGroupId: null, scope, includeAll: true, cancellationToken)
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
        ManagementCreateTagRequest request,
        CreateTagLabelCommandHandler handler,
        IUserLookupService userLookupService,
        ILogger<TagManagementEndpointsLoggingCategory> logger,
        CancellationToken cancellationToken
    )
    {
        if (request.NamespaceId == Guid.Empty)
        {
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { ["namespaceId"] = ["Namespace identifier is required."] }
            );
        }

        var claimedUserId = await principal
            .GetUserObjectIdAsync(userLookupService, cancellationToken)
            .ConfigureAwait(false);
        var createdBy = NormalizeGuid(request.CreatedBy) ?? claimedUserId;

        if (createdBy is null)
        {
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { ["createdBy"] = ["The creator could not be determined from the request or user context."] }
            );
        }

        var command = new CreateTagLabelCommand(
            request.NamespaceId,
            request.ParentId,
            request.Name,
            request.SortOrder,
            TagEndpointMapping.NormalizeColor(request.Color),
            TagEndpointMapping.NormalizeIcon(request.IconKey, TagEndpointMapping.ManagementDefaultIconKey),
            createdBy,
            request.IsSystem
        );
        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure || result.Value is null)
        {
            if (result.Errors.Count > 0)
            {
                logger.LogWarning(
                    "Failed to create managed tag label for namespace {NamespaceId} and name {Name}. Errors: {Errors}",
                    request.NamespaceId,
                    request.Name,
                    string.Join(", ", result.Errors)
                );
            }

            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { ["tag"] = [.. result.Errors] }
            );
        }

        var response = TagEndpointMapping.ToResponse(result.Value, TagEndpointMapping.ManagementDefaultIconKey);

        return TypedResults.Created($"/api/tag-management/tags/{response.Id}", response);
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

        return TypedResults.Created($"/api/tag-management/namespaces/{response.Id}", response);
    }

    private static async Task<Results<Ok<TagLabelResponse>, ValidationProblem, NotFound>> UpdateTagAsync(
        Guid tagId,
        ClaimsPrincipal principal,
        ManagementUpdateTagRequest request,
        UpdateTagLabelCommandHandler handler,
        IUserLookupService userLookupService,
        CancellationToken cancellationToken
    )
    {
        if (request.NamespaceId == Guid.Empty)
        {
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { ["namespaceId"] = ["Namespace identifier is required."] }
            );
        }

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
            TagEndpointMapping.NormalizeColor(request.Color),
            TagEndpointMapping.NormalizeIcon(request.IconKey, TagEndpointMapping.ManagementDefaultIconKey),
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
            TagEndpointMapping.ToResponse(result.Value, TagEndpointMapping.ManagementDefaultIconKey)
        );
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
}

internal sealed class TagManagementEndpointsLoggingCategory;
