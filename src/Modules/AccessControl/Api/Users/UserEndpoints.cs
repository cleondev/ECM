using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.AccessControl.Api.Roles;
using ECM.AccessControl.Application.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace ECM.AccessControl.Api.Users;

public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/access-control/users");
        group.WithTags("Access Control - Users");
        group.RequireAuthorization();

        group.MapGet("/", GetUsersAsync)
            .WithName("GetUsers")
            .WithSummary("List access control users");

        group.MapGet("/{id:guid}", GetUserByIdAsync)
            .WithName("GetUserById")
            .WithSummary("Get user details");

        group.MapPost("/", CreateUserAsync)
            .WithName("CreateUser")
            .WithSummary("Create a new user");

        group.MapPut("/{id:guid}", UpdateUserAsync)
            .WithName("UpdateUser")
            .WithSummary("Update user profile");

        group.MapPost("/{id:guid}/roles", AssignRoleAsync)
            .WithName("AssignRoleToUser")
            .WithSummary("Assign a role to a user");

        group.MapDelete("/{id:guid}/roles/{roleId:guid}", RemoveRoleAsync)
            .WithName("RemoveRoleFromUser")
            .WithSummary("Remove a role from a user");

        return group;
    }

    private static async Task<Ok<IReadOnlyCollection<UserResponse>>> GetUsersAsync(
        UserApplicationService service,
        CancellationToken cancellationToken)
    {
        var users = await service.GetAsync(cancellationToken);
        var response = users.Select(MapToResponse).ToArray();
        return TypedResults.Ok<IReadOnlyCollection<UserResponse>>(response);
    }

    private static async Task<Results<Ok<UserResponse>, NotFound>> GetUserByIdAsync(
        Guid id,
        UserApplicationService service,
        CancellationToken cancellationToken)
    {
        var user = await service.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(MapToResponse(user));
    }

    private static async Task<Results<Created<UserResponse>, ValidationProblem>> CreateUserAsync(
        CreateUserRequest request,
        UserApplicationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(
            new CreateUserCommand(
                request.Email,
                request.DisplayName,
                request.Department,
                request.IsActive,
                request.RoleIds),
            cancellationToken);

        if (result.IsFailure || result.Value is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["user"] = [.. result.Errors]
            });
        }

        var response = MapToResponse(result.Value);
        return TypedResults.Created($"/api/access-control/users/{response.Id}", response);
    }

    private static async Task<Results<Ok<UserResponse>, ValidationProblem, NotFound>> UpdateUserAsync(
        Guid id,
        UpdateUserRequest request,
        UserApplicationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(
            new UpdateUserCommand(
                id,
                request.DisplayName,
                request.Department,
                request.IsActive),
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Errors.Any(error => error.Contains("not found", StringComparison.OrdinalIgnoreCase)))
            {
                return TypedResults.NotFound();
            }

            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["user"] = [.. result.Errors]
            });
        }

        return TypedResults.Ok(MapToResponse(result.Value!));
    }

    private static async Task<Results<Ok<UserResponse>, ValidationProblem, NotFound>> AssignRoleAsync(
        Guid id,
        AssignRoleRequest request,
        UserApplicationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.AssignRoleAsync(
            new AssignUserRoleCommand(id, request.RoleId),
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Errors.Any(error => error.Contains("not found", StringComparison.OrdinalIgnoreCase)))
            {
                return TypedResults.NotFound();
            }

            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["user"] = [.. result.Errors]
            });
        }

        return TypedResults.Ok(MapToResponse(result.Value!));
    }

    private static async Task<Results<Ok<UserResponse>, ValidationProblem, NotFound>> RemoveRoleAsync(
        Guid id,
        Guid roleId,
        UserApplicationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.RemoveRoleAsync(
            new RemoveUserRoleCommand(id, roleId),
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Errors.Any(error => error.Contains("not found", StringComparison.OrdinalIgnoreCase)))
            {
                return TypedResults.NotFound();
            }

            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["user"] = [.. result.Errors]
            });
        }

        return TypedResults.Ok(MapToResponse(result.Value!));
    }

    private static UserResponse MapToResponse(UserSummary summary)
        => new(
            summary.Id,
            summary.Email,
            summary.DisplayName,
            summary.Department,
            summary.IsActive,
            summary.CreatedAtUtc,
            [.. summary.Roles.Select(role => new RoleResponse(role.Id, role.Name))]);
}
