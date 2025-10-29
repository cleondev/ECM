using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ECM.IAM.Api;
using ECM.IAM.Application.Users.Commands;
using ECM.IAM.Application.Users.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

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
        ILogger<LoggerCategory> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Handling profile retrieval for principal {Name} with auth type {AuthType}.",
            principal.Identity?.Name ?? "(unknown)",
            principal.Identity?.AuthenticationType ?? "(none)");

        var email = GetEmail(principal, logger);
        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning("Unable to resolve an email address for the current principal while handling profile retrieval.");
            LogPrincipalClaims(principal, logger);
            return TypedResults.NotFound();
        }

        logger.LogDebug("Resolved email {Email} for profile retrieval.", email);

        var summary = await handler.HandleAsync(new GetUserByEmailQuery(email), cancellationToken);
        if (summary is null)
        {
            logger.LogWarning("Profile lookup returned no result for email {Email}.", email);
            LogPrincipalClaims(principal, logger);
            return TypedResults.NotFound();
        }

        logger.LogInformation("Successfully retrieved profile for email {Email}.", email);
        return TypedResults.Ok(UserResponseMapper.Map(summary));
    }

    private static async Task<Results<Ok<UserResponse>, NotFound, ValidationProblem>> UpdateProfileAsync(
        UpdateUserProfileRequest request,
        ClaimsPrincipal principal,
        UpdateUserProfileCommandHandler handler,
        ILogger<LoggerCategory> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Handling profile update for principal {Name} with auth type {AuthType}.",
            principal.Identity?.Name ?? "(unknown)",
            principal.Identity?.AuthenticationType ?? "(none)");

        var email = GetEmail(principal, logger);
        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning("Unable to resolve an email address for the current principal while handling profile update.");
            LogPrincipalClaims(principal, logger);
            return TypedResults.NotFound();
        }

        logger.LogDebug(
            "Resolved email {Email} for profile update. Incoming display name length: {DisplayNameLength}; department provided: {HasDepartment}",
            email,
            request.DisplayName?.Length ?? 0,
            string.IsNullOrWhiteSpace(request.Department) ? "no" : "yes");

        var result = await handler.HandleAsync(
            new UpdateUserProfileCommand(email, request.DisplayName, request.Department),
            cancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning("Profile update failed for email {Email}. Errors: {Errors}", email, result.Errors);
            if (result.Errors.Any(error => error.Contains("not found", StringComparison.OrdinalIgnoreCase)))
            {
                return TypedResults.NotFound();
            }

            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["profile"] = [.. result.Errors]
            });
        }

        logger.LogInformation("Successfully updated profile for email {Email}.", email);
        return TypedResults.Ok(UserResponseMapper.Map(result.Value!));
    }

    private static string? GetEmail(ClaimsPrincipal principal, ILogger logger)
    {
        logger.LogDebug(
            "Attempting to resolve email for principal. Available claim types: {ClaimTypes}",
            principal.Claims.Select(claim => claim.Type).Distinct().ToArray());

        foreach (var candidate in GetPotentialEmailValues(principal))
        {
            var normalized = NormalizeEmailValue(candidate, logger);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                logger.LogDebug("Selected normalized email value with length {Length}.", normalized.Length);
                return normalized;
            }

            if (!string.IsNullOrWhiteSpace(candidate))
            {
                logger.LogTrace(
                    "Discarded candidate email value of length {Length} because it did not pass normalization.",
                    candidate.Length);
            }
        }

        logger.LogWarning("Failed to resolve email address from known claim types.");
        return null;
    }

    private static IEnumerable<string?> GetPotentialEmailValues(ClaimsPrincipal principal)
    {
        yield return principal.FindFirst(ClaimTypes.Email)?.Value;
        yield return principal.FindFirst("email")?.Value;

        foreach (var claim in principal.FindAll("emails"))
        {
            yield return claim.Value;
        }

        yield return principal.FindFirst("preferred_username")?.Value;
        yield return principal.FindFirst(ClaimTypes.Upn)?.Value;
    }

    private static string? NormalizeEmailValue(string? value, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();

        if (trimmed.StartsWith('['))
        {
            try
            {
                var emails = JsonSerializer.Deserialize<string[]>(trimmed);
                if (emails is not null)
                {
                    foreach (var email in emails)
                    {
                        var normalized = NormalizeEmailValue(email, logger);
                        if (!string.IsNullOrWhiteSpace(normalized))
                        {
                            return normalized;
                        }
                    }
                }
            }
            catch (JsonException exception)
            {
                logger.LogDebug(
                    exception,
                    "Failed to deserialize claim value as a JSON array when attempting to normalize an email.");
            }
        }

        return IsLikelyEmail(trimmed) ? trimmed : null;
    }

    private static void LogPrincipalClaims(ClaimsPrincipal principal, ILogger logger)
    {
        var claims = principal.Claims
            .Select(claim => new
            {
                claim.Type,
                ValuePreview = claim.Value.Length <= 32 ? claim.Value : $"{claim.Value[..32]}...",
                claim.ValueType
            })
            .ToArray();

        logger.LogDebug("Principal claims snapshot: {@Claims}", claims);
    }

    private static bool IsLikelyEmail(string value)
        => value.Contains('@');

    private sealed class LoggerCategory
    {
    }
}
