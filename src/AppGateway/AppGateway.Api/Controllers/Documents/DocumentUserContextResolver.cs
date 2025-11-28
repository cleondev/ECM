using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using AppGateway.Contracts.IAM.Groups;
using AppGateway.Infrastructure.Ecm;

using Microsoft.Extensions.Logging;

namespace AppGateway.Api.Controllers.Documents;

internal static class DocumentUserContextResolver
{
    public static async Task<(Guid? PrimaryGroupId, IReadOnlyList<Guid> GroupIds)> ResolveGroupSelectionAsync(
        IEcmApiClient client,
        ILogger logger,
        Guid createdBy,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await client.GetUserAsync(createdBy, cancellationToken);
            if (user is not null)
            {
                var normalized = DocumentRequestNormalization.NormalizeGroupSelection(
                    user.PrimaryGroupId,
                    user.GroupIds ?? [],
                    out var primaryGroupId);

                if (normalized.Count > 0)
                {
                    return (primaryGroupId, normalized);
                }

                logger.LogWarning(
                    "User {UserId} does not have any group memberships; falling back to the system group.",
                    createdBy);
            }
            else
            {
                logger.LogWarning(
                    "Failed to resolve user {UserId} for group selection; falling back to the system group.",
                    createdBy);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Failed to resolve group selection for user {UserId}; falling back to the system group.",
                createdBy);
        }

        return (GroupDefaultIds.System, new[] { GroupDefaultIds.System });
    }

    public static async Task<Guid?> ResolveUserIdAsync(
        IEcmApiClient client,
        ILogger logger,
        ClaimsPrincipal? principal,
        CancellationToken cancellationToken)
    {
        if (principal is null)
        {
            return null;
        }

        var upn = ResolveUserPrincipalName(principal);
        if (string.IsNullOrWhiteSpace(upn))
        {
            return null;
        }

        try
        {
            var user = await client.GetUserByEmailAsync(upn, cancellationToken);
            return user?.Id;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to resolve user id from UPN {Upn}", upn);
            return null;
        }
    }

    private static string? ResolveUserPrincipalName(ClaimsPrincipal principal)
    {
        foreach (var claimType in new[] { ClaimTypes.Upn, "preferred_username", ClaimTypes.Email })
        {
            var value = principal.FindFirst(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
