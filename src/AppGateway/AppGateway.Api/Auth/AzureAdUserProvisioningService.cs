namespace AppGateway.Api.Auth;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AppGateway.Contracts.IAM.Groups;
using AppGateway.Contracts.IAM.Roles;
using AppGateway.Contracts.IAM.Users;
using AppGateway.Infrastructure.IAM;
using AppGateway.Infrastructure.Ecm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public interface IUserProvisioningService
{
    Task<UserSummaryDto?> EnsureUserExistsAsync(ClaimsPrincipal? principal, CancellationToken cancellationToken);
}

public sealed class AzureAdUserProvisioningService(
    IUsersApiClient usersClient,
    IRolesApiClient rolesClient,
    IOptions<IamOptions> options,
    ILogger<AzureAdUserProvisioningService> logger) : IUserProvisioningService
{
    private readonly IUsersApiClient _usersClient = usersClient;
    private readonly IRolesApiClient _rolesClient = rolesClient;
    private readonly IOptions<IamOptions> _options = options;
    private readonly ILogger<AzureAdUserProvisioningService> _logger = logger;

    public async Task<UserSummaryDto?> EnsureUserExistsAsync(ClaimsPrincipal? principal, CancellationToken cancellationToken)
    {
        if (principal is null)
        {
            return null;
        }

        var email = GetEmail(principal);
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("Unable to automatically provision user because no email claim was found.");
            return null;
        }

        try
        {
            var existing = await _usersClient.GetUserByEmailAsync(email, cancellationToken);
            if (existing is not null)
            {
                return existing;
            }

            var displayName = GetDisplayName(principal, email);
            var primaryGroupIdClaim = GetPrimaryGroupId(principal);
            if (primaryGroupIdClaim.HasValue)
            {
                _logger.LogInformation(
                    "Resolved primary group {PrimaryGroupId} for user {Email} during provisioning.",
                    primaryGroupIdClaim.Value,
                    email);
            }

            var claimGroupIds = GetGroupIds(principal);
            if (claimGroupIds.Count > 0)
            {
                var summary = string.Join(
                    ", ",
                    claimGroupIds.Select(id => id.ToString()));

                _logger.LogInformation(
                    "Resolved {GroupCount} group assignments from claims for user {Email}: {Groups}.",
                    claimGroupIds.Count,
                    email,
                    summary);
            }

            var (groupIds, primaryGroupId) = BuildGroupSelection(claimGroupIds, primaryGroupIdClaim);
            var roleIds = await ResolveDefaultRoleIdsAsync(cancellationToken);

            if (groupIds.Count > 0)
            {
                var groupSummary = string.Join(
                    ", ",
                    groupIds.Select(id => id.ToString()));

                _logger.LogInformation(
                    "Automatically assigning IAM groups {Groups} to provisioned user {Email}.",
                    groupSummary,
                    email);
            }

            var request = new CreateUserRequestDto
            {
                Email = email,
                DisplayName = displayName,
                GroupIds = groupIds,
                PrimaryGroupId = primaryGroupId,
                IsActive = true,
                Password = null,
                RoleIds = roleIds
            };

            var created = await _usersClient.CreateUserAsync(request, cancellationToken);
            if (created is not null)
            {
                var createdRoles = created.Roles ?? [];
                var roleDescription = createdRoles.Count > 0
                    ? string.Join(", ", createdRoles.Select(role => role.Name))
                    : "<none>";

                _logger.LogInformation(
                    "Automatically provisioned user {Email} with roles: {Roles}.",
                    email,
                    roleDescription);

                return created;
            }

            _logger.LogWarning("Automatic provisioning failed for user {Email}.", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while provisioning user {Email}.", email);
        }

        return null;
    }

    private async Task<IReadOnlyCollection<Guid>> ResolveDefaultRoleIdsAsync(CancellationToken cancellationToken)
    {
        var configuration = _options.Value;
        var roleIds = new List<Guid>();

        if (configuration.DefaultRoleId.HasValue)
        {
            roleIds.Add(configuration.DefaultRoleId.Value);
        }

        if (!string.IsNullOrWhiteSpace(configuration.DefaultRoleName))
        {
            var roles = await _rolesClient.GetRolesAsync(cancellationToken);
            var match = roles.FirstOrDefault(role =>
                string.Equals(role.Name, configuration.DefaultRoleName, StringComparison.OrdinalIgnoreCase));

            if (match is not null && !roleIds.Contains(match.Id))
            {
                roleIds.Add(match.Id);
            }
            else if (match is null)
            {
                _logger.LogWarning(
                    "Configured default role '{RoleName}' was not found while provisioning a user.",
                    configuration.DefaultRoleName);
            }
        }

        return roleIds.Count > 0 ? roleIds : Array.Empty<Guid>();
    }

    private static string GetDisplayName(ClaimsPrincipal principal, string fallback)
        => principal.FindFirst("name")?.Value
           ?? principal.Identity?.Name
           ?? fallback;

    private static Guid? GetPrimaryGroupId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirst("primary_group_id")?.Value;
        return Guid.TryParse(value, out var parsed) && parsed != Guid.Empty ? parsed : null;
    }

    private static IReadOnlyCollection<Guid> GetGroupIds(ClaimsPrincipal principal)
    {
        var ids = new HashSet<Guid>();

        foreach (var claim in principal.FindAll("group_id"))
        {
            if (Guid.TryParse(claim.Value, out var parsed) && parsed != Guid.Empty)
            {
                ids.Add(parsed);
            }
        }

        return ids.Count > 0 ? ids.ToArray() : [];
    }

    private static (IReadOnlyCollection<Guid> GroupIds, Guid? PrimaryGroupId) BuildGroupSelection(
        IReadOnlyCollection<Guid> claimedGroupIds,
        Guid? primaryGroupId)
    {
        var groupIds = new List<Guid>
        {
            GroupDefaultIds.System,
            GroupDefaultIds.Guest
        };

        foreach (var groupId in claimedGroupIds)
        {
            if (!groupIds.Contains(groupId))
            {
                groupIds.Add(groupId);
            }
        }

        if (primaryGroupId.HasValue && !groupIds.Contains(primaryGroupId.Value))
        {
            groupIds.Add(primaryGroupId.Value);
        }

        return (groupIds, primaryGroupId);
    }

    private static string? GetEmail(ClaimsPrincipal principal)
    {
        foreach (var value in GetPotentialEmailValues(principal))
        {
            var email = NormalizeEmailValue(value);
            if (!string.IsNullOrWhiteSpace(email))
            {
                return email;
            }
        }

        return null;
    }

    private static IEnumerable<string?> GetPotentialEmailValues(ClaimsPrincipal principal)
    {
        yield return principal.FindFirst(ClaimTypes.Email)?.Value;
        yield return principal.FindFirst("email")?.Value;
        yield return principal.FindFirst("signInNames.emailAddress")?.Value;

        foreach (var claim in principal.FindAll("emails"))
        {
            yield return claim.Value;
        }

        yield return principal.FindFirst("preferred_username")?.Value;
        yield return principal.FindFirst(ClaimTypes.Upn)?.Value;
    }

    private static string? NormalizeEmailValue(string? value)
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
                        var normalized = NormalizeEmailValue(email);
                        if (!string.IsNullOrWhiteSpace(normalized))
                        {
                            return normalized;
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Ignore invalid JSON and fall through to the standard validation.
            }
        }

        return IsLikelyEmail(trimmed) ? trimmed : null;
    }

    private static bool IsLikelyEmail(string? value)
        => !string.IsNullOrWhiteSpace(value)
           && value.Contains('@')
           && value.IndexOf('@') < value.Length - 1;
}
