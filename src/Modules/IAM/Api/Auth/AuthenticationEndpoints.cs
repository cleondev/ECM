using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.IAM.Api.Users;
using ECM.IAM.Application.Users.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace ECM.IAM.Api.Auth;

public static class AuthenticationEndpoints
{
    public static RouteGroupBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/iam/auth");
        group.WithTags("IAM - Authentication");
        group.WithGroupName(IamSwagger.DocumentName);
        group.AllowAnonymous();

        group.MapPost("/login", AuthenticateAsync)
            .WithName("AuthenticateUser")
            .WithSummary("Authenticate a user with email and password");

        return group;
    }

    private static async Task<Results<Ok<UserResponse>, ValidationProblem, UnauthorizedHttpResult>> AuthenticateAsync(
        AuthenticateUserRequest request,
        AuthenticateUserQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors["email"] = ["Email is required."];
        }

        if (string.IsNullOrEmpty(request.Password))
        {
            errors["password"] = ["Password is required."];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var result = await handler.HandleAsync(
            new AuthenticateUserQuery(request.Email, request.Password),
            cancellationToken);

        if (result.IsFailure || result.Value is null)
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(UserResponseMapper.Map(result.Value));
    }
}
