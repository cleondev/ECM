using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.AccessControl.Api.Roles;
using ECM.AccessControl.Application.Users;
using ECM.AccessControl.Api;
using ECM.AccessControl.Application.Users.Commands;
using ECM.AccessControl.Application.Users.Queries;
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
        group.WithGroupName(AccessControlSwagger.DocumentName);
        group.RequireAuthorization();

        group.MapGet("/", GetUsersAsync)
            .WithName("GetUsers")
            .WithSummary("List access control users");

        group.MapGet("/{id:guid}", GetUserByIdAsync)
            .WithName("GetUserById")
            .WithSummary("Get user details");

        group.MapGet("/by-email", GetUserByEmailAsync)
            .WithName("GetUserByEmail")
            .WithSummary("Get user details by email address");

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
        GetUsersQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var users = await handler.HandleAsync(new GetUsersQuery(), cancellationToken);
        var response = users.Select(UserResponseMapper.Map).ToArray();
        return TypedResults.Ok<IReadOnlyCollection<UserResponse>>(response);
    }

    private static async Task<Results<Ok<UserResponse>, NotFound>> GetUserByIdAsync(
        Guid id,
        GetUserByIdQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var user = await handler.HandleAsync(new GetUserByIdQuery(id), cancellationToken);
        if (user is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(UserResponseMapper.Map(user));
    }

    private static async Task<Results<Ok<UserResponse>, ValidationProblem, NotFound>> GetUserByEmailAsync(
        string email,
        GetUserByEmailQueryHandler handler,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["email"] = ["Email address is required."]
            });
        }

        var user = await handler.HandleAsync(new GetUserByEmailQuery(email), cancellationToken);
        if (user is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(UserResponseMapper.Map(user));
    }

    private static async Task<Results<Created<UserResponse>, ValidationProblem>> CreateUserAsync(
        CreateUserRequest request,
        CreateUserCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
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

        var response = UserResponseMapper.Map(result.Value);
        return TypedResults.Created($"/api/access-control/users/{response.Id}", response);
    }

    private static async Task<Results<Ok<UserResponse>, ValidationProblem, NotFound>> UpdateUserAsync(
        Guid id,
        UpdateUserRequest request,
        UpdateUserCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
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

        return TypedResults.Ok(UserResponseMapper.Map(result.Value!));
    }

    private static async Task<Results<Ok<UserResponse>, ValidationProblem, NotFound>> AssignRoleAsync(
        Guid id,
        AssignRoleRequest request,
        AssignUserRoleCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
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

        return TypedResults.Ok(UserResponseMapper.Map(result.Value!));
    }

    private static async Task<Results<Ok<UserResponse>, ValidationProblem, NotFound>> RemoveRoleAsync(
        Guid id,
        Guid roleId,
        RemoveUserRoleCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
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

        return TypedResults.Ok(UserResponseMapper.Map(result.Value!));
    }
}
