using ECM.Document.Domain.Tags;
using ECM.Document.Infrastructure.Persistence;
using ECM.Document.Infrastructure.Tags;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Documents;
using Xunit;

namespace Document.Tests.Infrastructure.Tags;

public class TagLabelRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsOutboxMessageForCreatedTag()
    {
        var options = new DbContextOptionsBuilder<DocumentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new DocumentDbContext(options);
        context.TagNamespaces.Add(new TagNamespace("system", "system", null, "System", DateTimeOffset.UtcNow));
        await context.SaveChangesAsync();

        var repository = new TagLabelRepository(context);
        var tagLabel = TagLabel.Create("system", "reviewed", "reviewed", Guid.NewGuid(), DateTimeOffset.UtcNow);

        await repository.AddAsync(tagLabel, CancellationToken.None);

        var message = Assert.Single(context.OutboxMessages);
        Assert.Equal("tag", message.Aggregate);
        Assert.Equal(nameof(TagLabelCreatedContract), message.Type);
    }

    [Fact]
    public async Task RemoveAsync_PersistsOutboxMessageForDeletedTag()
    {
        var options = new DbContextOptionsBuilder<DocumentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new DocumentDbContext(options);
        context.TagNamespaces.Add(new TagNamespace("system", "system", null, "System", DateTimeOffset.UtcNow));

        var tagLabel = TagLabel.Create("system", "reviewed", "reviewed", Guid.NewGuid(), DateTimeOffset.UtcNow);
        tagLabel.ClearDomainEvents();
        context.TagLabels.Add(tagLabel);
        await context.SaveChangesAsync();
        context.OutboxMessages.RemoveRange(context.OutboxMessages);
        await context.SaveChangesAsync();

        var repository = new TagLabelRepository(context);
        tagLabel.MarkDeleted(DateTimeOffset.UtcNow);

        await repository.RemoveAsync(tagLabel, CancellationToken.None);

        var message = Assert.Single(context.OutboxMessages);
        Assert.Equal(nameof(TagLabelDeletedContract), message.Type);
    }
}
