using ECM.AccessControl.Domain.Relations;
using ECM.AccessControl.Infrastructure.Persistence;
using ECM.AccessControl.Infrastructure.Relations;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.AccessControl;
using Xunit;

namespace AccessControl.Test.Infrastructure.Relations;

public class AccessRelationRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsOutboxMessageForRelationCreated()
    {
        var options = new DbContextOptionsBuilder<AccessControlDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AccessControlDbContext(options);
        var repository = new AccessRelationRepository(context);

        var relation = AccessRelation.Create(Guid.NewGuid(), "document", Guid.NewGuid(), "owner", DateTimeOffset.UtcNow);

        await repository.AddAsync(relation, CancellationToken.None);

        var message = Assert.Single(context.OutboxMessages);
        Assert.Equal(AccessControlEventNames.AccessRelationCreated, message.Type);
    }

    [Fact]
    public async Task DeleteAsync_PersistsOutboxMessageForRelationDeleted()
    {
        var options = new DbContextOptionsBuilder<AccessControlDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AccessControlDbContext(options);
        var repository = new AccessRelationRepository(context);

        var relation = AccessRelation.Create(Guid.NewGuid(), "document", Guid.NewGuid(), "owner", DateTimeOffset.UtcNow);
        await repository.AddAsync(relation, CancellationToken.None);

        context.OutboxMessages.RemoveRange(context.OutboxMessages);
        await context.SaveChangesAsync();

        relation.MarkDeleted(DateTimeOffset.UtcNow);

        await repository.DeleteAsync(relation, CancellationToken.None);

        var message = Assert.Single(context.OutboxMessages);
        Assert.Equal(AccessControlEventNames.AccessRelationDeleted, message.Type);
    }
}
