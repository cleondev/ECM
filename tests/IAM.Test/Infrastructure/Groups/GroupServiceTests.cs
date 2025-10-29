using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.IAM.Application.Groups;
using ECM.IAM.Domain.Groups;
using ECM.IAM.Domain.Users;
using ECM.IAM.Infrastructure.Groups;
using ECM.IAM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IAM.Test.Infrastructure.Groups;

public class GroupServiceTests
{
    [Fact]
    public async Task EnsureUserGroupsAsync_AssignsParentToNewGroup()
    {
        var clock = new FixedClock(new DateTimeOffset(2025, 3, 10, 12, 0, 0, TimeSpan.Zero));
        var options = new DbContextOptionsBuilder<IamDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new IamDbContext(options);
        var service = new GroupService(context, clock, NullLogger<GroupService>.Instance);

        var parentGroup = Group.Create("parent", GroupKind.Unit, createdBy: null, clock.UtcNow);
        context.Groups.Add(parentGroup);

        var user = User.Create("new.user@example.com", "New User", clock.UtcNow);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var assignments = new[] { GroupAssignment.Unit("child", parentGroup.Id) };

        await service.EnsureUserGroupsAsync(user, assignments, CancellationToken.None);

        var childGroup = await context.Groups.SingleAsync(group => group.Name == "child");
        Assert.Equal(parentGroup.Id, childGroup.ParentGroupId);
    }

    [Fact]
    public async Task EnsureUserGroupsAsync_UpdatesParentForExistingGroup()
    {
        var clock = new FixedClock(new DateTimeOffset(2025, 3, 10, 12, 0, 0, TimeSpan.Zero));
        var options = new DbContextOptionsBuilder<IamDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new IamDbContext(options);
        var service = new GroupService(context, clock, NullLogger<GroupService>.Instance);

        var originalParent = Group.Create("parent-a", GroupKind.Unit, createdBy: null, clock.UtcNow);
        var newParent = Group.Create("parent-b", GroupKind.Unit, createdBy: null, clock.UtcNow);
        var child = Group.Create("child", GroupKind.Unit, createdBy: null, clock.UtcNow, originalParent.Id);
        context.Groups.AddRange(originalParent, newParent, child);

        var user = User.Create("existing.user@example.com", "Existing User", clock.UtcNow);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var assignments = new[] { new GroupAssignment("child", GroupKind.Unit, newParent.Id) };

        await service.EnsureUserGroupsAsync(user, assignments, CancellationToken.None);

        var updatedGroup = await context.Groups.SingleAsync(group => group.Id == child.Id);
        Assert.Equal(newParent.Id, updatedGroup.ParentGroupId);
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
