using ECM.AccessControl.Domain.Roles;
using ECM.AccessControl.Domain.Users;
using ECM.AccessControl.Infrastructure.Persistence;
using ECM.AccessControl.Infrastructure.Users;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.AccessControl;
using Xunit;

namespace AccessControl.Test.Infrastructure.Users;

public class UserRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsOutboxMessageForUserCreated()
    {
        var options = new DbContextOptionsBuilder<AccessControlDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AccessControlDbContext(options);
        var repository = new UserRepository(context);

        var user = User.Create("alice@example.com", "Alice", DateTimeOffset.UtcNow);

        await repository.AddAsync(user, CancellationToken.None);

        var message = Assert.Single(context.OutboxMessages);
        Assert.Equal("user", message.Aggregate);
        Assert.Equal(AccessControlEventNames.UserCreated, message.Type);
    }

    [Fact]
    public async Task UpdateAsync_PersistsOutboxMessageForRoleAssignment()
    {
        var options = new DbContextOptionsBuilder<AccessControlDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AccessControlDbContext(options);
        var repository = new UserRepository(context);

        var role = Role.Create("Reviewer");
        context.Roles.Add(role);

        var user = User.Create("bob@example.com", "Bob", DateTimeOffset.UtcNow);
        await repository.AddAsync(user, CancellationToken.None);

        context.OutboxMessages.RemoveRange(context.OutboxMessages);
        await context.SaveChangesAsync();

        user.AssignRole(role, DateTimeOffset.UtcNow);

        await repository.UpdateAsync(user, CancellationToken.None);

        var message = Assert.Single(context.OutboxMessages);
        Assert.Equal(AccessControlEventNames.UserRoleAssigned, message.Type);
    }
}
