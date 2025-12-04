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

        await service.EnsureUserGroupsAsync(user, assignments, null, CancellationToken.None);

        var childGroup = await context.Groups.SingleAsync(group => group.Name == "child");
        Assert.Equal(parentGroup.Id, childGroup.ParentGroupId);
    }

    [Fact]
    public async Task EnsureUserGroupsAsync_CreatesTeamGroupFromAssignment()
    {
        var clock = new FixedClock(new DateTimeOffset(2025, 3, 10, 12, 0, 0, TimeSpan.Zero));
        var options = new DbContextOptionsBuilder<IamDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new IamDbContext(options);
        var service = new GroupService(context, clock, NullLogger<GroupService>.Instance);

        var user = User.Create("team.user@example.com", "Team User", clock.UtcNow);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var assignments = new[] { GroupAssignment.FromContract(null, "beta-team", "team") };

        await service.EnsureUserGroupsAsync(user, assignments, null, CancellationToken.None);

        var teamGroup = await context.Groups.SingleAsync(group => group.Name == "beta-team");
        Assert.Equal(GroupKind.Team, teamGroup.Kind);

        var membership = await context.GroupMembers.SingleAsync(member => member.GroupId == teamGroup.Id && member.UserId == user.Id);
        Assert.Null(membership.ValidToUtc);
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

        var assignments = new[] { new GroupAssignment(null, "child", GroupKind.Unit, newParent.Id) };

        await service.EnsureUserGroupsAsync(user, assignments, null, CancellationToken.None);

        var updatedGroup = await context.Groups.SingleAsync(group => group.Id == child.Id);
        Assert.Equal(newParent.Id, updatedGroup.ParentGroupId);
    }

    [Fact]
    public async Task EnsureUserGroupsAsync_CreatesSystemGroup_WhenAssignmentUsesWellKnownId()
    {
        var clock = new FixedClock(new DateTimeOffset(2025, 3, 10, 12, 0, 0, TimeSpan.Zero));
        var options = new DbContextOptionsBuilder<IamDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new IamDbContext(options);
        var service = new GroupService(context, clock, NullLogger<GroupService>.Instance);

        var user = User.Create("system.user@example.com", "System User", clock.UtcNow);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var assignments = new[] { GroupAssignment.System() };

        await service.EnsureUserGroupsAsync(user, assignments, null, CancellationToken.None);

        var systemGroup = await context.Groups.SingleAsync(group => group.Id == GroupDefaults.SystemId);
        Assert.Equal(GroupDefaults.SystemName, systemGroup.Name);

        var membership = await context.GroupMembers.SingleAsync(member => member.GroupId == systemGroup.Id && member.UserId == user.Id);
        Assert.Null(membership.ValidToUtc);
    }

    [Fact]
    public async Task EnsureUserGroupsAsync_CreatesGuessGroupAndSetsAsPrimary()
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
        var guessGroup = await context.Groups.SingleAsync(group => group.Kind == GroupKind.Guess);

        Assert.Equal(systemGroup.Id, guessGroup.ParentGroupId);
        Assert.Equal(GroupDefaults.GuestName, guessGroup.Name);
        Assert.Equal(guessGroup.Id, user.PrimaryGroupId);

        var membership = await context.GroupMembers.SingleAsync(member => member.GroupId == guessGroup.Id && member.UserId == user.Id);
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
