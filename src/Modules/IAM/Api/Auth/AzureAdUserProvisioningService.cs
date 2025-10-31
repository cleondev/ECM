namespace ECM.IAM.Api.Auth;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.IAM.Application.Groups;
using ECM.IAM.Application.Roles;
using ECM.IAM.Application.Users;
using ECM.IAM.Domain.Groups;
using ECM.IAM.Domain.Roles;
using ECM.IAM.Domain.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public interface IUserProvisioningService
{
    Task EnsureUserExistsAsync(ClaimsPrincipal? principal, CancellationToken cancellationToken);
}

public sealed class IamProvisioningOptions
{
    public const string SectionName = "IAM";

    public Guid? DefaultRoleId { get; init; }

    public string? DefaultRoleName { get; init; }
}

public sealed class AzureAdUserProvisioningService(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IGroupService groupService,
    ISystemClock clock,
    IOptions<IamProvisioningOptions> options,
    ILogger<AzureAdUserProvisioningService> logger) : IUserProvisioningService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IRoleRepository _roleRepository = roleRepository;
    private readonly IGroupService _groupService = groupService;
    private readonly ISystemClock _clock = clock;
    private readonly IOptions<IamProvisioningOptions> _options = options;
    private readonly ILogger<AzureAdUserProvisioningService> _logger = logger;

    public async Task EnsureUserExistsAsync(ClaimsPrincipal? principal, CancellationToken cancellationToken)
    {
        if (principal is null)
        {
            return;
        }

        var email = GetEmail(principal);
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("Unable to automatically provision user because no email claim was found.");
            return;
        }

        try
        {
            var existing = await _userRepository.GetByEmailAsync(email, cancellationToken);
            var primaryGroupId = GetPrimaryGroupId(principal);
            if (primaryGroupId.HasValue)
            {
                _logger.LogInformation(
                    "Resolved primary group {PrimaryGroupId} for user {Email} during provisioning.",
                    primaryGroupId.Value,
                    email);
            }

            var claimedGroupIds = GetGroupIds(principal);
            if (claimedGroupIds.Count > 0)
            {
                var summary = string.Join(
                    ", ",
                    claimedGroupIds.Select(id => id.ToString()));

                _logger.LogInformation(
                    "Resolved {GroupCount} group assignments from claims for user {Email}: {Groups}.",
                    claimedGroupIds.Count,
                    email,
                    summary);
            }

            var assignments = BuildAssignments(claimedGroupIds, primaryGroupId);

            if (existing is not null)
            {
                await _groupService.EnsureUserGroupsAsync(existing, assignments, primaryGroupId, cancellationToken);
                return;
            }

            var displayName = GetDisplayName(principal, email);
            var roles = await ResolveDefaultRolesAsync(cancellationToken);

            var user = User.Create(email, displayName, _clock.UtcNow, isActive: true);

            if (roles.Count > 0)
            {
                foreach (var role in roles)
                {
                    user.AssignRole(role, _clock.UtcNow);
                }
            }

            await _userRepository.AddAsync(user, cancellationToken);

            await _groupService.EnsureUserGroupsAsync(user, assignments, primaryGroupId, cancellationToken);

            var roleDescription = roles.Count > 0
                ? string.Join(", ", roles.Select(role => role.Name))
                : "<none>";

            _logger.LogInformation(
                "Automatically provisioned user {Email} with roles: {Roles}.",
                email,
                roleDescription);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An error occurred while provisioning user {Email}.", email);
        }
    }

    private async Task<IReadOnlyCollection<Role>> ResolveDefaultRolesAsync(CancellationToken cancellationToken)
    {
        var configuration = _options.Value;
        var roles = new List<Role>();

        if (configuration.DefaultRoleId.HasValue)
        {
            var role = await _roleRepository.GetByIdAsync(configuration.DefaultRoleId.Value, cancellationToken);
            if (role is not null)
            {
                roles.Add(role);
            }
            else
            {
                _logger.LogWarning(
                    "Configured default role '{RoleId}' was not found while provisioning a user.",
                    configuration.DefaultRoleId.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(configuration.DefaultRoleName))
        {
            var role = await _roleRepository.GetByNameAsync(configuration.DefaultRoleName, cancellationToken);
            if (role is not null && roles.All(existing => existing.Id != role.Id))
            {
                roles.Add(role);
            }
            else if (role is null)
            {
                _logger.LogWarning(
                    "Configured default role '{RoleName}' was not found while provisioning a user.",
                    configuration.DefaultRoleName);
            }
        }

        return roles.Count > 0 ? roles : Array.Empty<Role>();
    }

    private static string GetDisplayName(ClaimsPrincipal principal, string fallback)
        => principal.FindFirst("name")?.Value
           ?? principal.Identity?.Name
           ?? fallback;

    private static string? GetEmail(ClaimsPrincipal principal)
        => principal.FindFirst(ClaimTypes.Email)?.Value
           ?? principal.FindFirst("email")?.Value
           ?? principal.FindFirst("preferred_username")?.Value
           ?? principal.FindFirst("signInNames.emailAddress")?.Value
           ?? principal.FindFirst("emails")?.Value
           ?? principal.FindFirst(ClaimTypes.Upn)?.Value;

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

    private static IReadOnlyCollection<GroupAssignment> BuildAssignments(
        IReadOnlyCollection<Guid> claimedGroupIds,
        Guid? primaryGroupId)
    {
        var assignments = new List<GroupAssignment>
        {
            GroupAssignment.System(),
            GroupAssignment.Guest(),
        };

        foreach (var groupId in claimedGroupIds)
        {
            if (groupId == Guid.Empty)
            {
                continue;
            }

            if (groupId == GroupDefaults.SystemId || groupId == GroupDefaults.GuestId)
            {
                continue;
            }

            assignments.Add(GroupAssignment.ForExistingGroup(groupId));
        }

        if (primaryGroupId.HasValue
            && primaryGroupId.Value != Guid.Empty
            && assignments.All(assignment => assignment.GroupId != primaryGroupId.Value))
        {
            assignments.Add(GroupAssignment.ForExistingGroup(primaryGroupId.Value));
        }

        return assignments;
    }
}
