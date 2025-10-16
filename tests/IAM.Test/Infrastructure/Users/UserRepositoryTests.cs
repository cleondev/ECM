using ECM.IAM.Domain.Roles;
using ECM.IAM.Domain.Users;
using ECM.IAM.Infrastructure.Persistence;
using ECM.IAM.Infrastructure.Users;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.IAM;
using Xunit;

namespace IAM.Test.Infrastructure.Users;

public class UserRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsOutboxMessageForUserCreated()
    {
        var options = new DbContextOptionsBuilder<IamDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new IamDbContext(options);
        var repository = new UserRepository(context);

        var user = User.Create("alice@example.com", "Alice", DateTimeOffset.UtcNow);

        await repository.AddAsync(user, CancellationToken.None);

        var message = Assert.Single(context.OutboxMessages);
        Assert.Equal("user", message.Aggregate);
        Assert.Equal(IamEventNames.UserCreated, message.Type);
    }

    [Fact]
    public async Task UpdateAsync_PersistsOutboxMessageForRoleAssignment()
    {
        var options = new DbContextOptionsBuilder<IamDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new IamDbContext(options);
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
        Assert.Equal(IamEventNames.UserRoleAssigned, message.Type);
    }
}
