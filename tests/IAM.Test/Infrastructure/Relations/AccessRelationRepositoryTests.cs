using ECM.IAM.Domain.Relations;
using ECM.IAM.Infrastructure.Persistence;
using ECM.IAM.Infrastructure.Relations;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.IAM;
using Xunit;

namespace IAM.Test.Infrastructure.Relations;

public class AccessRelationRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsOutboxMessageForRelationCreated()
    {
        var options = new DbContextOptionsBuilder<IamDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new IamDbContext(options);
        var repository = new AccessRelationRepository(context);

        var relation = AccessRelation.Create("user", Guid.NewGuid(), "document", Guid.NewGuid(), "owner", DateTimeOffset.UtcNow);

        await repository.AddAsync(relation, CancellationToken.None);

        var message = Assert.Single(context.OutboxMessages);
        Assert.Equal(IamEventNames.AccessRelationCreated, message.Type);
    }

    [Fact]
    public async Task DeleteAsync_PersistsOutboxMessageForRelationDeleted()
    {
        var options = new DbContextOptionsBuilder<IamDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new IamDbContext(options);
        var repository = new AccessRelationRepository(context);

        var relation = AccessRelation.Create("user", Guid.NewGuid(), "document", Guid.NewGuid(), "owner", DateTimeOffset.UtcNow);
        await repository.AddAsync(relation, CancellationToken.None);

        context.OutboxMessages.RemoveRange(context.OutboxMessages);
        await context.SaveChangesAsync();

        relation.MarkDeleted(DateTimeOffset.UtcNow);

        await repository.DeleteAsync(relation, CancellationToken.None);

        var message = Assert.Single(context.OutboxMessages);
        Assert.Equal(IamEventNames.AccessRelationDeleted, message.Type);
    }
}
