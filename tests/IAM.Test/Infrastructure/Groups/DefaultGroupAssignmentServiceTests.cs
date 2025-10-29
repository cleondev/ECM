using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.IAM.Domain.Users;
using ECM.IAM.Infrastructure.Groups;
using ECM.IAM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TestFixtures;
using Xunit;

namespace IAM.Test.Infrastructure.Groups;

public class DefaultGroupAssignmentServiceTests
{
    private readonly DefaultGroupFixture _groups = new();

    [Fact]
    public async Task AssignAsync_CreatesMissingGroupsAndMemberships()
    {
        var clock = new FixedClock(new DateTimeOffset(2025, 1, 15, 8, 30, 0, TimeSpan.Zero));
        var options = new DbContextOptionsBuilder<IamDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new IamDbContext(options);
        var service = new DefaultGroupAssignmentService(context, clock, NullLogger<DefaultGroupAssignmentService>.Instance);

        var user = User.Create("guest@example.com", "Guest", clock.UtcNow);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        await service.AssignAsync(user, CancellationToken.None);

        var groupNames = await context.Groups
            .OrderBy(group => group.Name)
            .Select(group => group.Name)
            .ToListAsync();

        Assert.Equal(new[] { _groups.GuestGroupName, _groups.SystemGroupName }, groupNames);

        var assignedGroupNames = await context.GroupMembers
            .Include(member => member.Group)
            .Where(member => member.UserId == user.Id)
            .Select(member => member.Group.Name)
            .OrderBy(name => name)
            .ToListAsync();

        Assert.Equal(new[] { _groups.GuestGroupName, _groups.SystemGroupName }, assignedGroupNames);
    }

    [Fact]
    public async Task AssignAsync_WhenCalledMultipleTimes_IsIdempotent()
    {
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var options = new DbContextOptionsBuilder<IamDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new IamDbContext(options);
        var service = new DefaultGroupAssignmentService(context, clock, NullLogger<DefaultGroupAssignmentService>.Instance);

        var user = User.Create("system@example.com", "System", clock.UtcNow);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        await service.AssignAsync(user, CancellationToken.None);
        await service.AssignAsync(user, CancellationToken.None);

        var assignments = await context.GroupMembers
            .Where(member => member.UserId == user.Id)
            .ToListAsync();

        Assert.Equal(2, assignments.Count);
        Assert.All(assignments, assignment => Assert.Null(assignment.ValidToUtc));
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
