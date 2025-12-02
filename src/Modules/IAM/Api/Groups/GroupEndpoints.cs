using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.IAM.Domain.Groups;
using ECM.IAM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace ECM.IAM.Api.Groups;

public static class GroupEndpoints
{
    public static RouteGroupBuilder MapGroupEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/iam/groups");
        group.WithGroupName(IamSwagger.DocumentName);
        group.WithTags("Groups");

        group
            .MapGet("/", ListGroupsAsync)
            .WithName("ListGroups")
            .WithDescription("List all IAM groups available to administrators.");

        group
            .MapPost("/", CreateGroupAsync)
            .WithName("CreateGroup")
            .WithDescription("Create a new IAM group.");

        group
            .MapPut("/{id:guid}", UpdateGroupAsync)
            .WithName("UpdateGroup")
            .WithDescription("Update IAM group details.");

        group
            .MapDelete("/{id:guid}", DeleteGroupAsync)
            .WithName("DeleteGroup")
            .WithDescription("Delete an IAM group.");

        return group;
    }

    private static async Task<Ok<GroupResponse[]>> ListGroupsAsync(
        IamDbContext context,
        CancellationToken cancellationToken)
    {
        var groups = await context.Groups
            .AsNoTracking()
            .OrderBy(group => group.Name)
            .ToListAsync(cancellationToken);

        var response = groups
            .Select(group => new GroupResponse(
                group.Id,
                group.Name,
                group.Kind.ToNormalizedString(),
                Role: string.Empty,
                group.ParentGroupId))
            .ToArray();

        return TypedResults.Ok(response);
    }

    private static async Task<Results<Created<GroupResponse>, ValidationProblem>> CreateGroupAsync(
        CreateGroupRequest request,
        IamDbContext context,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateGroupRequest(request);
        if (validationErrors.Count > 0)
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        Guid? parentGroupId = NormalizeParentId(request.ParentGroupId);
        if (parentGroupId.HasValue && !await context.Groups.AnyAsync(group => group.Id == parentGroupId, cancellationToken))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.ParentGroupId)] = ["Parent group does not exist."]
            });
        }

        var group = Group.Create(request.Name, request.Kind, createdBy: null, DateTimeOffset.UtcNow, parentGroupId);

        await context.Groups.AddAsync(group, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var response = new GroupResponse(
            group.Id,
            group.Name,
            group.Kind.ToNormalizedString(),
            Role: string.Empty,
            group.ParentGroupId);

        return TypedResults.Created($"/api/iam/groups/{group.Id}", response);
    }

    private static async Task<Results<Ok<GroupResponse>, ValidationProblem, NotFound>> UpdateGroupAsync(
        Guid id,
        UpdateGroupRequest request,
        IamDbContext context,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateGroupRequest(request);
        if (validationErrors.Count > 0)
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        var group = await context.Groups.FirstOrDefaultAsync(group => group.Id == id, cancellationToken);
        if (group is null)
        {
            return TypedResults.NotFound();
        }

        Guid? parentGroupId = NormalizeParentId(request.ParentGroupId);
        if (parentGroupId.HasValue && !await context.Groups.AnyAsync(parent => parent.Id == parentGroupId, cancellationToken))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.ParentGroupId)] = ["Parent group does not exist."]
            });
        }

        group.Rename(request.Name);
        group.SetKind(GroupKindExtensions.FromString(request.Kind));
        group.SetParent(parentGroupId);

        await context.SaveChangesAsync(cancellationToken);

        var response = new GroupResponse(
            group.Id,
            group.Name,
            group.Kind.ToNormalizedString(),
            Role: string.Empty,
            group.ParentGroupId);

        return TypedResults.Ok(response);
    }

    private static async Task<Results<NoContent, NotFound>> DeleteGroupAsync(
        Guid id,
        IamDbContext context,
        CancellationToken cancellationToken)
    {
        var group = await context.Groups.FirstOrDefaultAsync(group => group.Id == id, cancellationToken);
        if (group is null)
        {
            return TypedResults.NotFound();
        }

        context.Groups.Remove(group);
        await context.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static Dictionary<string, string[]> ValidateGroupRequest(GroupMutationRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors[nameof(request.Name)] = ["Group name is required."];
        }

        return errors;
    }

    private static Guid? NormalizeParentId(Guid? parentGroupId)
        => parentGroupId.HasValue && parentGroupId.Value != Guid.Empty ? parentGroupId : null;
}

public abstract record GroupMutationRequest(string Name, string? Kind, Guid? ParentGroupId);

public sealed record CreateGroupRequest(string Name, string? Kind, Guid? ParentGroupId) : GroupMutationRequest(Name, Kind, ParentGroupId);

public sealed record UpdateGroupRequest(string Name, string? Kind, Guid? ParentGroupId) : GroupMutationRequest(Name, Kind, ParentGroupId);
