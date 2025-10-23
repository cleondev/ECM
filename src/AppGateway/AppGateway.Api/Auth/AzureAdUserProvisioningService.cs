namespace AppGateway.Api.Auth;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
    IEcmApiClient client,
    IOptions<IamOptions> options,
    ILogger<AzureAdUserProvisioningService> logger) : IUserProvisioningService
{
    private readonly IEcmApiClient _client = client;
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
            var existing = await _client.GetUserByEmailAsync(email, cancellationToken);
            if (existing is not null)
            {
                return existing;
            }

            var displayName = GetDisplayName(principal, email);
            var department = GetDepartment(principal);
            var roleIds = await ResolveDefaultRoleIdsAsync(cancellationToken);

            var request = new CreateUserRequestDto
            {
                Email = email,
                DisplayName = displayName,
                Department = department,
                IsActive = true,
                RoleIds = roleIds
            };

            var created = await _client.CreateUserAsync(request, cancellationToken);
            if (created is not null)
            {
                var createdRoles = created.Roles ?? Array.Empty<RoleSummaryDto>();
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
            var roles = await _client.GetRolesAsync(cancellationToken);
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

    private static string? GetDepartment(ClaimsPrincipal principal)
    {
        var value = principal.FindFirst("department")?.Value;
        return string.IsNullOrWhiteSpace(value) ? null : value;
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
           && value.IndexOf('@') > 0
           && value.IndexOf('@') < value.Length - 1;
}
