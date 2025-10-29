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
    private static readonly IReadOnlyList<string> DefaultGroupNames = GroupDefaults.Names;

    private readonly IamDbContext _context = context;
    private readonly ISystemClock _clock = clock;
    private readonly ILogger<DefaultGroupAssignmentService> _logger = logger;

    public async Task AssignAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var now = _clock.UtcNow;

        var existingGroups = await _context.Groups
            .Where(group => DefaultGroupNames.Contains(group.Name))
            .ToListAsync(cancellationToken);

        foreach (var missingName in DefaultGroupNames.Except(existingGroups.Select(group => group.Name)))
        {
            var group = Group.CreateSystemGroup(missingName, now);
            existingGroups.Add(group);
            await _context.Groups.AddAsync(group, cancellationToken);
            _logger.LogInformation("Created default IAM group {GroupName}.", group.Name);
        }

        var groupIds = existingGroups.Select(group => group.Id).ToArray();

        var existingMemberships = await _context.GroupMembers
            .Where(member => member.UserId == user.Id && groupIds.Contains(member.GroupId))
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
}
