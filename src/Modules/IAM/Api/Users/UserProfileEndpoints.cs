using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ECM.IAM.Api;
using ECM.IAM.Application.Users.Commands;
using ECM.IAM.Application.Users.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace ECM.IAM.Api.Users;

public static class UserProfileEndpoints
{
    public static RouteGroupBuilder MapUserProfileEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/iam/profile");
        group.WithTags("IAM - Profile");
        group.WithGroupName(IamSwagger.DocumentName);
        group.RequireAuthorization();

        group.MapGet(string.Empty, GetProfileAsync)
            .WithName("GetCurrentUserProfile")
            .WithSummary("Get the profile of the current user");

        group.MapPut(string.Empty, UpdateProfileAsync)
            .WithName("UpdateCurrentUserProfile")
            .WithSummary("Update the profile of the current user");

        return group;
    }

    private static async Task<Results<Ok<UserResponse>, NotFound>> GetProfileAsync(
        ClaimsPrincipal principal,
        GetUserByEmailQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var email = GetEmail(principal);
        if (string.IsNullOrWhiteSpace(email))
        {
            return TypedResults.NotFound();
        }

        var summary = await handler.HandleAsync(new GetUserByEmailQuery(email), cancellationToken);
        if (summary is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(UserResponseMapper.Map(summary));
    }

    private static async Task<Results<Ok<UserResponse>, NotFound, ValidationProblem>> UpdateProfileAsync(
        UpdateUserProfileRequest request,
        ClaimsPrincipal principal,
        UpdateUserProfileCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var email = GetEmail(principal);
        if (string.IsNullOrWhiteSpace(email))
        {
            return TypedResults.NotFound();
        }

        var result = await handler.HandleAsync(
            new UpdateUserProfileCommand(email, request.DisplayName, request.Department),
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Errors.Any(error => error.Contains("not found", StringComparison.OrdinalIgnoreCase)))
            {
                return TypedResults.NotFound();
            }

            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["profile"] = [.. result.Errors]
            });
        }

        return TypedResults.Ok(UserResponseMapper.Map(result.Value!));
    }

    private static string? GetEmail(ClaimsPrincipal principal)
        => principal.FindFirst(ClaimTypes.Email)?.Value
           ?? principal.FindFirst("preferred_username")?.Value
           ?? principal.FindFirst("emails")?.Value
           ?? principal.FindFirst(ClaimTypes.Upn)?.Value;
}
