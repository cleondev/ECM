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
    public async Task EnsureUserGroupsAsync_AddsDefaultGroups_WhenAssignmentsAreEmpty()
    {
        var clock = new FixedClock(new DateTimeOffset(2025, 3, 10, 12, 0, 0, TimeSpan.Zero));
        var options = new DbContextOptionsBuilder<IamDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new IamDbContext(options);
        var service = new GroupService(context, clock, NullLogger<GroupService>.Instance);

        var user = User.Create("guess.user@example.com", "Guess User", clock.UtcNow);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        await service.EnsureUserGroupsAsync(user, [], null, CancellationToken.None);

        var systemGroup = await context.Groups.SingleAsync(group => group.Id == GroupDefaults.SystemId);
        var guestGroup = await context.Groups.SingleAsync(group => group.Kind == GroupKind.Guess);

        Assert.Equal(systemGroup.Id, guestGroup.ParentGroupId);
        Assert.Equal(GroupDefaults.GuestName, guestGroup.Name);
        Assert.Equal(guestGroup.Id, user.PrimaryGroupId);

        Assert.True(await context.GroupMembers.AnyAsync(member => member.GroupId == systemGroup.Id && member.UserId == user.Id));
        Assert.True(await context.GroupMembers.AnyAsync(member => member.GroupId == guestGroup.Id && member.UserId == user.Id));
    }

    [Fact]
    public async Task EnsureUserGroupsAsync_CreatesGroupsFromAssignments()
    {
        var clock = new FixedClock(new DateTimeOffset(2025, 3, 10, 12, 0, 0, TimeSpan.Zero));
        var options = new DbContextOptionsBuilder<IamDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new IamDbContext(options);
        var service = new GroupService(context, clock, NullLogger<GroupService>.Instance);

        var parentGroup = Group.Create("parent", GroupKind.Unit, createdBy: null, clock.UtcNow);
        context.Groups.Add(parentGroup);

        var user = User.Create("team.user@example.com", "Team User", clock.UtcNow);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var assignments = new[] { GroupAssignment.Unit("child", parentGroup.Id) };

        await service.EnsureUserGroupsAsync(user, assignments, null, CancellationToken.None);

        var childGroup = await context.Groups.SingleAsync(group => group.Name == "child");
        Assert.Equal(parentGroup.Id, childGroup.ParentGroupId);

        var membership = await context.GroupMembers.SingleAsync(member => member.GroupId == childGroup.Id && member.UserId == user.Id);
        Assert.Null(membership.ValidToUtc);
    }

    [Fact]
    public async Task EnsureUserGroupsAsync_ReopensExistingMembership()
    {
        var clock = new FixedClock(new DateTimeOffset(2025, 3, 10, 12, 0, 0, TimeSpan.Zero));
        var options = new DbContextOptionsBuilder<IamDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new IamDbContext(options);
        var service = new GroupService(context, clock, NullLogger<GroupService>.Instance);

        var group = Group.Create("engineering", GroupKind.Unit, createdBy: null, clock.UtcNow);
        context.Groups.Add(group);

        var user = User.Create("existing.user@example.com", "Existing User", clock.UtcNow);
        context.Users.Add(user);

        var closedMembership = GroupMember.Create(group.Id, user.Id, clock.UtcNow.AddDays(-2));
        closedMembership.Close(clock.UtcNow.AddDays(-1));
        context.GroupMembers.Add(closedMembership);

        await context.SaveChangesAsync();

        var assignments = new[] { GroupAssignment.ForExistingGroup(group.Id) };

        await service.EnsureUserGroupsAsync(user, assignments, null, CancellationToken.None);

        var membership = await context.GroupMembers.SingleAsync(member => member.GroupId == group.Id && member.UserId == user.Id);
        Assert.Null(membership.ValidToUtc);
    }

    [Fact]
    public async Task EnsureUserGroupsAsync_SetsPrimaryGroup_WhenProvided()
    {
        var clock = new FixedClock(new DateTimeOffset(2025, 3, 10, 12, 0, 0, TimeSpan.Zero));
        var options = new DbContextOptionsBuilder<IamDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new IamDbContext(options);
        var service = new GroupService(context, clock, NullLogger<GroupService>.Instance);

        var group = Group.Create("engineering", GroupKind.Unit, createdBy: null, clock.UtcNow);
        context.Groups.Add(group);

        var user = User.Create("primary.user@example.com", "Primary User", clock.UtcNow);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var assignments = new[] { GroupAssignment.ForExistingGroup(group.Id) };

        await service.EnsureUserGroupsAsync(user, assignments, group.Id, CancellationToken.None);

        Assert.Equal(group.Id, user.PrimaryGroupId);
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
