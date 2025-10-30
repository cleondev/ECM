namespace ECM.IAM.Infrastructure.Groups;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.IAM.Application.Groups;
using ECM.IAM.Domain.Groups;
using ECM.IAM.Domain.Users;
using ECM.IAM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public sealed class GroupService(
    IamDbContext context,
    ISystemClock clock,
    ILogger<GroupService> logger) : IGroupService
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    private readonly IamDbContext _context = context;
    private readonly ISystemClock _clock = clock;
    private readonly ILogger<GroupService> _logger = logger;

    public async Task EnsureUserGroupsAsync(
        User user,
        IReadOnlyCollection<GroupAssignment> assignments,
        Guid? primaryGroupId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(assignments);

        var normalizedAssignments = assignments
            .Where(assignment => assignment is not null)
            .Select(assignment => assignment.Normalize())
            .ToList();

        var normalizedPrimaryGroupId = primaryGroupId.HasValue && primaryGroupId.Value != Guid.Empty
            ? primaryGroupId.Value
            : (Guid?)null;

        var systemGroup = await EnsureSystemGroupExistsAsync(cancellationToken);
        var guessGroup = await EnsureGuessGroupExistsAsync(systemGroup.Id, cancellationToken);

        if (!normalizedPrimaryGroupId.HasValue && !user.PrimaryGroupId.HasValue)
        {
            normalizedPrimaryGroupId = guessGroup.Id;
        }

        if (normalizedPrimaryGroupId.HasValue
            && normalizedAssignments.All(assignment => assignment.GroupId != normalizedPrimaryGroupId))
        {
            var primaryAssignment = normalizedAssignments
                .FirstOrDefault(assignment => assignment.GroupId == normalizedPrimaryGroupId);
            var primaryKind = primaryAssignment?.Kind
                ?? (normalizedPrimaryGroupId == guessGroup.Id ? guessGroup.Kind : GroupKind.System);

            normalizedAssignments.Add(
                GroupAssignment.ForExistingGroup(normalizedPrimaryGroupId.Value, primaryKind).Normalize());
        }

        if (normalizedAssignments.All(assignment => assignment.GroupId != systemGroup.Id))
        {
            normalizedAssignments.Add(
                GroupAssignment.ForExistingGroup(systemGroup.Id, systemGroup.Kind).Normalize());
        }

        if (normalizedAssignments.All(assignment => assignment.GroupId != guessGroup.Id))
        {
            normalizedAssignments.Add(
                GroupAssignment.ForExistingGroup(guessGroup.Id, guessGroup.Kind).Normalize());
        }

        var dedupedAssignments = new List<GroupAssignment>();
        var seenGroupIds = new HashSet<Guid>();
        var seenIdentifiers = new HashSet<string>(Comparer);

        foreach (var assignment in normalizedAssignments)
        {
            if (assignment.GroupId.HasValue)
            {
                if (seenGroupIds.Add(assignment.GroupId.Value))
                {
                    dedupedAssignments.Add(assignment);
                }

                continue;
            }

            var identifierKey = BuildIdentifierKey(assignment.Identifier, assignment.ParentGroupId);
            if (identifierKey is not null)
            {
                if (seenIdentifiers.Add(identifierKey))
                {
                    dedupedAssignments.Add(assignment);
                }

                continue;
            }

            dedupedAssignments.Add(assignment);
        }

        normalizedAssignments = dedupedAssignments;

        var targetGroupIds = normalizedAssignments
            .Where(assignment => assignment.GroupId.HasValue)
            .Select(assignment => assignment.GroupId!.Value)
            .Distinct()
            .ToArray();

        var identifierTargets = normalizedAssignments
            .Where(assignment => !string.IsNullOrWhiteSpace(assignment.Identifier))
            .Select(assignment => assignment.Identifier!.Trim())
            .Distinct(Comparer)
            .ToArray();

        var existingGroupsById = targetGroupIds.Length == 0
            ? []
            : await _context.Groups
                .Where(group => targetGroupIds.Contains(group.Id))
                .ToDictionaryAsync(group => group.Id, cancellationToken);

        var existingGroupsByIdentifier = identifierTargets.Length == 0
            ? new Dictionary<string, Group>(Comparer)
            : (await _context.Groups
                    .Where(group => identifierTargets.Contains(group.Name))
                    .ToListAsync(cancellationToken))
                .ToDictionary(group => group.Name, Comparer);

        if (!existingGroupsById.ContainsKey(systemGroup.Id))
        {
            existingGroupsById[systemGroup.Id] = systemGroup;
        }

        if (!existingGroupsByIdentifier.ContainsKey(systemGroup.Name))
        {
            existingGroupsByIdentifier[systemGroup.Name] = systemGroup;
        }

        if (!existingGroupsById.ContainsKey(guessGroup.Id))
        {
            existingGroupsById[guessGroup.Id] = guessGroup;
        }

        if (!existingGroupsByIdentifier.ContainsKey(guessGroup.Name))
        {
            existingGroupsByIdentifier[guessGroup.Name] = guessGroup;
        }

        var unitTargetIds = new HashSet<Guid>();

        foreach (var assignment in normalizedAssignments)
        {
            var desiredParentGroupId = assignment.ParentGroupId;
            Group? group = null;

            if (assignment.GroupId.HasValue && existingGroupsById.TryGetValue(assignment.GroupId.Value, out group))
            {
                var identifier = assignment.Identifier?.Trim();
                if (!string.IsNullOrWhiteSpace(identifier) && !existingGroupsByIdentifier.ContainsKey(identifier))
                {
                    existingGroupsByIdentifier[identifier] = group;
                }
            }
            else if (assignment.GroupId.HasValue)
            {
                var desiredGroupId = assignment.GroupId.Value;
                var identifier = assignment.Identifier?.Trim();
                var createdNewGroup = false;

                if (!string.IsNullOrWhiteSpace(identifier)
                    && existingGroupsByIdentifier.TryGetValue(identifier, out group))
                {
                    existingGroupsById[group.Id] = group;

                    if (group.Id != desiredGroupId)
                    {
                        _logger.LogInformation(
                            "Mapped requested group id {RequestedGroupId} to existing group {GroupId} via identifier {Identifier} for user {UserId}.",
                            desiredGroupId,
                            group.Id,
                            identifier,
                            user.Id);
                    }
                }
                else if (desiredGroupId == GroupDefaults.SystemId)
                {
                    group = Group.CreateSystemGroup(GroupDefaults.SystemName, _clock.UtcNow);
                    createdNewGroup = true;
                }
                else if (desiredGroupId == GroupDefaults.GuestId)
                {
                    group = Group.CreateSystemGroup(GroupDefaults.GuestName, _clock.UtcNow);
                    createdNewGroup = true;
                }
                else if (!string.IsNullOrWhiteSpace(identifier))
                {
                    group = Group.Create(identifier, assignment.Kind, createdBy: null, _clock.UtcNow, desiredParentGroupId);
                    createdNewGroup = true;
                }
                else
                {
                    _logger.LogWarning(
                        "Skipping group assignment for user {UserId} because group id {GroupId} could not be resolved.",
                        user.Id,
                        desiredGroupId);
                    continue;
                }

                if (group is null)
                {
                    continue;
                }

                if (createdNewGroup)
                {
                    await _context.Groups.AddAsync(group, cancellationToken);
                    existingGroupsById[group.Id] = group;
                    existingGroupsByIdentifier[group.Name] = group;
                    _logger.LogInformation(
                        "Created IAM group {GroupName} of kind {Kind} while provisioning user {UserId}.",
                        group.Name,
                        group.Kind.ToNormalizedString(),
                        user.Id);
                }
                else
                {
                    existingGroupsById[group.Id] = group;
                    if (!existingGroupsByIdentifier.ContainsKey(group.Name))
                    {
                        existingGroupsByIdentifier[group.Name] = group;
                    }
                }
            }
            else
            {
                var identifier = assignment.Identifier?.Trim();
                if (string.IsNullOrWhiteSpace(identifier))
                {
                    _logger.LogWarning(
                        "Skipping group assignment without identifier for user {UserId} while provisioning.",
                        user.Id);
                    continue;
                }

                if (!existingGroupsByIdentifier.TryGetValue(identifier, out group))
                {
                    group = Group.Create(identifier, assignment.Kind, createdBy: null, _clock.UtcNow, desiredParentGroupId);
                    await _context.Groups.AddAsync(group, cancellationToken);
                    existingGroupsByIdentifier[identifier] = group;
                    existingGroupsById[group.Id] = group;
                    _logger.LogInformation(
                        "Created IAM group {GroupName} of kind {Kind} while provisioning user {UserId}.",
                        group.Name,
                        group.Kind.ToNormalizedString(),
                        user.Id);
                }
            }

            if (group.ParentGroupId != desiredParentGroupId)
            {
                group.SetParent(desiredParentGroupId);
            }

            var isMember = await _context.GroupMembers.AnyAsync(
                member => member.GroupId == group.Id && member.UserId == user.Id && member.ValidToUtc == null,
                cancellationToken);

            if (!isMember)
            {
                var membership = GroupMember.Create(group.Id, user.Id, _clock.UtcNow, assignment.Role);
                await _context.GroupMembers.AddAsync(membership, cancellationToken);
                _logger.LogInformation(
                    "Added user {UserId} to group {GroupName}.",
                    user.Id,
                    group.Name);
            }

            if (assignment.Kind is GroupKind.Unit or GroupKind.Team)
            {
                unitTargetIds.Add(group.Id);
            }
        }

        var activeMemberships = await _context.GroupMembers
            .Include(member => member.Group)
            .Where(member => member.UserId == user.Id && member.ValidToUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var membership in activeMemberships.Where(
                     member => member.Group is not null
                         && member.Group.Kind is GroupKind.Unit or GroupKind.Team))
        {
            if (!unitTargetIds.Contains(membership.GroupId))
            {
                membership.Close(_clock.UtcNow);
                _logger.LogInformation(
                    "Marked membership of user {UserId} in unit group {GroupName} as inactive.",
                    user.Id,
                    membership.Group!.Name);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        var refreshedMemberships = await _context.GroupMembers
            .Include(member => member.Group)
            .Where(member => member.UserId == user.Id && member.ValidToUtc == null)
            .ToListAsync(cancellationToken);

        user.SyncGroups(refreshedMemberships);

        Guid? resolvedPrimaryGroupId = null;

        if (normalizedPrimaryGroupId.HasValue
            && refreshedMemberships.Any(member => member.GroupId == normalizedPrimaryGroupId.Value))
        {
            resolvedPrimaryGroupId = normalizedPrimaryGroupId.Value;
        }
        else if (user.PrimaryGroupId.HasValue
            && refreshedMemberships.Any(member => member.GroupId == user.PrimaryGroupId.Value))
        {
            resolvedPrimaryGroupId = user.PrimaryGroupId;
        }
        else
        {
            resolvedPrimaryGroupId = refreshedMemberships
                .Where(member => member.Group is not null && member.Group.Kind == GroupKind.Guess)
                .Select(member => (Guid?)member.GroupId)
                .FirstOrDefault()
                ?? refreshedMemberships
                    .Where(
                        member => member.Group is not null
                            && member.Group.Kind is GroupKind.Unit or GroupKind.Team)
                    .Select(member => (Guid?)member.GroupId)
                    .FirstOrDefault();
        }

        var existingPrimaryGroupId = user.PrimaryGroupId;
        user.SetPrimaryGroup(resolvedPrimaryGroupId);

        if (existingPrimaryGroupId != user.PrimaryGroupId)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<Group> EnsureSystemGroupExistsAsync(CancellationToken cancellationToken)
    {
        var group = await _context.Groups
            .FirstOrDefaultAsync(existing => existing.Id == GroupDefaults.SystemId, cancellationToken);

        group ??= await _context.Groups
            .FirstOrDefaultAsync(existing => existing.Kind == GroupKind.System, cancellationToken);

        if (group is not null)
        {
            return group;
        }

        group = Group.CreateSystemGroup(GroupDefaults.SystemName, _clock.UtcNow);
        await _context.Groups.AddAsync(group, cancellationToken);
        _logger.LogInformation(
            "Created IAM group {GroupName} of kind {Kind} while ensuring defaults.",
            group.Name,
            group.Kind.ToNormalizedString());

        return group;
    }

    private async Task<Group> EnsureGuessGroupExistsAsync(Guid systemGroupId, CancellationToken cancellationToken)
    {
        var group = await _context.Groups
            .FirstOrDefaultAsync(existing => existing.Id == GroupDefaults.GuessUserId, cancellationToken);

        group ??= await _context.Groups
            .FirstOrDefaultAsync(existing => existing.Kind == GroupKind.Guess, cancellationToken);

        if (group is null)
        {
            group = Group.CreateGuessGroup(systemGroupId, _clock.UtcNow);
            await _context.Groups.AddAsync(group, cancellationToken);
            _logger.LogInformation(
                "Created IAM group {GroupName} of kind {Kind} while ensuring defaults.",
                group.Name,
                group.Kind.ToNormalizedString());

            return group;
        }

        if (group.ParentGroupId != systemGroupId)
        {
            group.SetParent(systemGroupId);
        }

        if (!string.Equals(group.Name, GroupDefaults.GuessUserName, StringComparison.OrdinalIgnoreCase))
        {
            group.Rename(GroupDefaults.GuessUserName);
        }

        return group;
    }

    private static string? BuildIdentifierKey(string? identifier, Guid? parentGroupId)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return null;
        }

        return $"{identifier.Trim()}::{parentGroupId?.ToString() ?? string.Empty}";
    }
}
