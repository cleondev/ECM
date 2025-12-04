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

        var systemGroup = await EnsureSystemGroupExistsAsync(cancellationToken);
        var guestGroup = await EnsureGuessGroupExistsAsync(systemGroup.Id, cancellationToken);

        if (normalizedAssignments.Count == 0)
        {
            normalizedAssignments.Add(GroupAssignment.ForExistingGroup(systemGroup.Id, systemGroup.Kind));
            normalizedAssignments.Add(GroupAssignment.ForExistingGroup(guestGroup.Id, guestGroup.Kind));
        }

        var memberships = await _context.GroupMembers
            .Include(member => member.Group)
            .Where(member => member.UserId == user.Id)
            .ToListAsync(cancellationToken);

        var resolvedGroups = new Dictionary<Guid, Group>();

        foreach (var assignment in normalizedAssignments)
        {
            var group = await ResolveGroupAsync(assignment, user.Id, systemGroup, guestGroup, cancellationToken);
            if (group is null)
            {
                continue;
            }

            resolvedGroups[group.Id] = group;

            var existingMembership = memberships.FirstOrDefault(member => member.GroupId == group.Id);
            if (existingMembership is null)
            {
                var membership = GroupMember.Create(group.Id, user.Id, _clock.UtcNow, assignment.Role);
                await _context.GroupMembers.AddAsync(membership, cancellationToken);
                memberships.Add(membership);
                _logger.LogInformation("Added user {UserId} to group {GroupName}.", user.Id, group.Name);
                continue;
            }

            if (existingMembership.ValidToUtc.HasValue)
            {
                existingMembership.Reopen(_clock.UtcNow, assignment.Role);
                _logger.LogInformation("Reactivated membership of user {UserId} in group {GroupName}.", user.Id, group.Name);
            }
        }

        if (resolvedGroups.Count == 0)
        {
            resolvedGroups[systemGroup.Id] = systemGroup;
            resolvedGroups[guestGroup.Id] = guestGroup;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var refreshedMemberships = await _context.GroupMembers
            .Include(member => member.Group)
            .Where(member => member.UserId == user.Id && member.ValidToUtc == null)
            .ToListAsync(cancellationToken);

        user.SyncGroups(refreshedMemberships);

        var resolvedPrimaryGroupId = DeterminePrimaryGroupId(
            primaryGroupId,
            user,
            refreshedMemberships,
            guestGroup.Id,
            resolvedGroups.Keys);

        var existingPrimaryGroupId = user.PrimaryGroupId;
        user.SetPrimaryGroup(resolvedPrimaryGroupId);

        if (existingPrimaryGroupId != user.PrimaryGroupId)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private Guid? DeterminePrimaryGroupId(
        Guid? requestedPrimaryGroupId,
        User user,
        IReadOnlyCollection<GroupMember> memberships,
        Guid guestGroupId,
        IEnumerable<Guid> resolvedGroupIds)
    {
        if (requestedPrimaryGroupId.HasValue
            && memberships.Any(member => member.GroupId == requestedPrimaryGroupId))
        {
            return requestedPrimaryGroupId;
        }

        if (user.PrimaryGroupId.HasValue
            && memberships.Any(member => member.GroupId == user.PrimaryGroupId))
        {
            return user.PrimaryGroupId;
        }

        if (resolvedGroupIds.Contains(guestGroupId))
        {
            return guestGroupId;
        }

        return memberships.FirstOrDefault()?.GroupId;
    }

    private async Task<Group?> ResolveGroupAsync(
        GroupAssignment assignment,
        Guid userId,
        Group systemGroup,
        Group guestGroup,
        CancellationToken cancellationToken)
    {
        if (Comparer.Equals(assignment.Identifier, systemGroup.Name))
        {
            return systemGroup;
        }

        if (Comparer.Equals(assignment.Identifier, guestGroup.Name))
        {
            return guestGroup;
        }

        Group? group = null;

        if (assignment.GroupId.HasValue)
        {
            group = await _context.Groups
                .FirstOrDefaultAsync(existing => existing.Id == assignment.GroupId.Value, cancellationToken);
        }

        var identifier = assignment.Identifier?.Trim();

        if (group is null && !string.IsNullOrWhiteSpace(identifier))
        {
            group = await _context.Groups
                .FirstOrDefaultAsync(existing => existing.Name == identifier, cancellationToken);
        }

        if (group is null)
        {
            if (assignment.GroupId == GroupDefaults.SystemId)
            {
                return systemGroup;
            }

            if (assignment.GroupId == GroupDefaults.GuestId)
            {
                return guestGroup;
            }

            if (string.IsNullOrWhiteSpace(identifier))
            {
                _logger.LogWarning(
                    "Skipping group assignment for user {UserId} because no identifier or known group id was provided.",
                    userId);
                return null;
            }

            group = Group.Create(identifier, assignment.Kind, createdBy: null, _clock.UtcNow, assignment.ParentGroupId);
            await _context.Groups.AddAsync(group, cancellationToken);
            _logger.LogInformation(
                "Created IAM group {GroupName} of kind {Kind} while provisioning user {UserId}.",
                group.Name,
                group.Kind.ToNormalizedString(),
                userId);
        }

        if (assignment.ParentGroupId.HasValue && group.ParentGroupId != assignment.ParentGroupId)
        {
            group.SetParent(assignment.ParentGroupId);
        }

        return group;
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
            .FirstOrDefaultAsync(existing => existing.Id == GroupDefaults.GuestId, cancellationToken);

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

        if (!string.Equals(group.Name, GroupDefaults.GuestName, StringComparison.OrdinalIgnoreCase))
        {
            group.Rename(GroupDefaults.GuestName);
        }

        return group;
    }
}
