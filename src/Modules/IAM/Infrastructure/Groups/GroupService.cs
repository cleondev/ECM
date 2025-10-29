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
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(assignments);

        var normalizedAssignments = assignments
            .Where(assignment => assignment is not null)
            .Select(assignment => assignment.Normalize())
            .GroupBy(assignment => assignment.Name, Comparer)
            .Select(group => group.First())
            .ToArray();

        if (normalizedAssignments.Length == 0)
        {
            user.SetDepartment(null);
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        var targetNames = normalizedAssignments.Select(assignment => assignment.Name).ToArray();

        var existingGroups = await _context.Groups
            .Where(group => targetNames.Contains(group.Name))
            .ToDictionaryAsync(group => group.Name, Comparer, cancellationToken);

        foreach (var assignment in normalizedAssignments)
        {
            if (!existingGroups.TryGetValue(assignment.Name, out var group))
            {
                group = Group.Create(assignment.Name, assignment.Kind, createdBy: null, _clock.UtcNow);
                await _context.Groups.AddAsync(group, cancellationToken);
                existingGroups[assignment.Name] = group;
                _logger.LogInformation("Created IAM group {GroupName} of kind {Kind} while provisioning user {UserId}.", assignment.Name, assignment.Kind, user.Id);
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
                    assignment.Name);
            }
        }

        var activeMemberships = await _context.GroupMembers
            .Include(member => member.Group)
            .Where(member => member.UserId == user.Id && member.ValidToUtc == null)
            .ToListAsync(cancellationToken);

        var unitTargets = normalizedAssignments
            .Where(assignment => string.Equals(assignment.Kind, "unit", StringComparison.OrdinalIgnoreCase))
            .Select(assignment => assignment.Name)
            .ToHashSet(Comparer);

        foreach (var membership in activeMemberships.Where(member => member.Group.Kind == "unit"))
        {
            if (!unitTargets.Contains(membership.Group.Name))
            {
                membership.Close(_clock.UtcNow);
                _logger.LogInformation(
                    "Marked membership of user {UserId} in unit group {GroupName} as inactive.",
                    user.Id,
                    membership.Group.Name);
            }
        }

        user.SetDepartment(unitTargets.FirstOrDefault());

        await _context.SaveChangesAsync(cancellationToken);

        var refreshedMemberships = await _context.GroupMembers
            .Include(member => member.Group)
            .Where(member => member.UserId == user.Id && member.ValidToUtc == null)
            .ToListAsync(cancellationToken);

        user.SyncGroups(refreshedMemberships);
    }
}
