using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.AccessControl.Application.Roles.Commands;
using ECM.AccessControl.Application.Roles.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace ECM.AccessControl.Api.Roles;

public static class RoleEndpoints
{
    public static RouteGroupBuilder MapRoleEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/access-control/roles");
        group.WithTags("Access Control - Roles");
        group.RequireAuthorization();

        group.MapGet("/", GetRolesAsync)
            .WithName("GetRoles")
            .WithSummary("List roles");

        group.MapGet("/{id:guid}", GetRoleByIdAsync)
            .WithName("GetRoleById")
            .WithSummary("Get role details");

        group.MapPost("/", CreateRoleAsync)
            .WithName("CreateRole")
            .WithSummary("Create a new role");

        group.MapPut("/{id:guid}", RenameRoleAsync)
            .WithName("RenameRole")
            .WithSummary("Rename a role");

        group.MapDelete("/{id:guid}", DeleteRoleAsync)
            .WithName("DeleteRole")
            .WithSummary("Delete a role");

        return group;
    }

    private static async Task<Ok<IReadOnlyCollection<RoleResponse>>> GetRolesAsync(
        GetRolesQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var roles = await handler.HandleAsync(new GetRolesQuery(), cancellationToken);
        var response = roles.Select(role => new RoleResponse(role.Id, role.Name)).ToArray();
        return TypedResults.Ok<IReadOnlyCollection<RoleResponse>>(response);
    }

    private static async Task<Results<Ok<RoleResponse>, NotFound>> GetRoleByIdAsync(
        Guid id,
        GetRoleByIdQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var role = await handler.HandleAsync(new GetRoleByIdQuery(id), cancellationToken);
        if (role is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new RoleResponse(role.Id, role.Name));
    }

    private static async Task<Results<Created<RoleResponse>, ValidationProblem>> CreateRoleAsync(
        CreateRoleRequest request,
        CreateRoleCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new CreateRoleCommand(request.Name), cancellationToken);
        if (result.IsFailure || result.Value is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["role"] = [.. result.Errors]
            });
        }

        var response = new RoleResponse(result.Value.Id, result.Value.Name);
        return TypedResults.Created($"/api/access-control/roles/{response.Id}", response);
    }

    private static async Task<Results<Ok<RoleResponse>, ValidationProblem, NotFound>> RenameRoleAsync(
        Guid id,
        RenameRoleRequest request,
        RenameRoleCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new RenameRoleCommand(id, request.Name), cancellationToken);
        if (result.IsFailure)
        {
            if (result.Errors.Any(error => error.Contains("not found", StringComparison.OrdinalIgnoreCase)))
            {
                return TypedResults.NotFound();
            }

            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["role"] = [.. result.Errors]
            });
        }

        return TypedResults.Ok(new RoleResponse(result.Value!.Id, result.Value.Name));
    }

    private static async Task<Results<NoContent, NotFound>> DeleteRoleAsync(
        Guid id,
        DeleteRoleCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var deleted = await handler.HandleAsync(new DeleteRoleCommand(id), cancellationToken);
        if (!deleted)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.NoContent();
    }
}
