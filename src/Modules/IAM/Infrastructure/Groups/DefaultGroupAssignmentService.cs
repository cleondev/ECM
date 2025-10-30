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

namespace ECM.IAM.Infrastructure.Groups;

public sealed class DefaultGroupAssignmentService(
    IamDbContext context,
    ISystemClock clock,
    ILogger<DefaultGroupAssignmentService> logger) : IDefaultGroupAssignmentService
{
    private readonly IamDbContext _context = context;
    private readonly ISystemClock _clock = clock;
    private readonly ILogger<DefaultGroupAssignmentService> _logger = logger;

    public async Task AssignAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var now = _clock.UtcNow;

        var defaultGroupNames = new[]
        {
            GroupDefaults.SystemName,
            GroupDefaults.GuestName,
            GroupDefaults.GuessUserName,
        };

        var existingGroups = await _context.Groups
            .Where(group => defaultGroupNames.Contains(group.Name))
            .ToDictionaryAsync(group => group.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var systemGroup = await EnsureSystemGroupAsync(existingGroups, now, cancellationToken);
        var guestGroup = await EnsureGuestGroupAsync(existingGroups, now, cancellationToken);
        var guessGroup = await EnsureGuessGroupAsync(existingGroups, systemGroup, now, cancellationToken);

        var trackedGroups = new[] { systemGroup, guestGroup, guessGroup };

        var groupIds = trackedGroups.Select(group => group.Id).ToArray();

        var existingMemberships = await _context.GroupMembers
            .Where(member => member.UserId == user.Id && groupIds.Contains(member.GroupId) && member.ValidToUtc == null)
            .Select(member => member.GroupId)
            .ToListAsync(cancellationToken);

        var assignments = groupIds
            .Except(existingMemberships)
            .Select(groupId => GroupMember.Create(groupId, user.Id, now))
            .ToArray();

        if (assignments.Length == 0)
        {
            if (_context.ChangeTracker.HasChanges())
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        await _context.GroupMembers.AddRangeAsync(assignments, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        foreach (var assignment in assignments)
        {
            _logger.LogInformation(
                "Assigned user {UserId} to IAM group {GroupId}.",
                assignment.UserId,
                assignment.GroupId);
        }
    }

    private async Task<Group> EnsureSystemGroupAsync(
        IDictionary<string, Group> existingGroups,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (existingGroups.TryGetValue(GroupDefaults.SystemName, out var group))
        {
            return group;
        }

        group = Group.CreateSystemGroup(GroupDefaults.SystemName, now);
        await _context.Groups.AddAsync(group, cancellationToken);
        existingGroups[GroupDefaults.SystemName] = group;
        _logger.LogInformation("Created default IAM group {GroupName}.", group.Name);

        return group;
    }

    private async Task<Group> EnsureGuestGroupAsync(
        IDictionary<string, Group> existingGroups,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (existingGroups.TryGetValue(GroupDefaults.GuestName, out var group))
        {
            return group;
        }

        group = Group.CreateSystemGroup(GroupDefaults.GuestName, now);
        await _context.Groups.AddAsync(group, cancellationToken);
        existingGroups[GroupDefaults.GuestName] = group;
        _logger.LogInformation("Created default IAM group {GroupName}.", group.Name);

        return group;
    }

    private async Task<Group> EnsureGuessGroupAsync(
        IDictionary<string, Group> existingGroups,
        Group systemGroup,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (existingGroups.TryGetValue(GroupDefaults.GuessUserName, out var group))
        {
            if (group.ParentGroupId != systemGroup.Id)
            {
                group.SetParent(systemGroup.Id);
            }

            return group;
        }

        group = Group.CreateGuessGroup(systemGroup.Id, now);
        await _context.Groups.AddAsync(group, cancellationToken);
        existingGroups[GroupDefaults.GuessUserName] = group;
        _logger.LogInformation("Created default IAM group {GroupName}.", group.Name);

        return group;
    }
}
