using System;
using System.Threading;
using System.Threading.Tasks;
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
        var now = DateTimeOffset.UtcNow;
        var tagNamespace = TagNamespace.Create("global", null, null, "System", isSystem: true, createdAtUtc: now);
        context.TagNamespaces.Add(tagNamespace);
        await context.SaveChangesAsync();

        var repository = new TagLabelRepository(context);
        var tagLabel = TagLabel.Create(
            tagNamespace.Id,
            parentId: null,
            parentPathIds: [],
            name: "reviewed",
            sortOrder: 0,
            color: null,
            iconKey: null,
            createdBy: Guid.NewGuid(),
            isSystem: false,
            createdAtUtc: now);

        await repository.AddAsync(tagLabel, CancellationToken.None);

        var message = Assert.Single(context.OutboxMessages);
        Assert.Equal("tag-label", message.Aggregate);
        Assert.Equal(DocumentEventNames.TagLabelCreated, message.Type);
    }

    [Fact]
    public async Task RemoveAsync_PersistsOutboxMessageForDeletedTag()
    {
        var options = new DbContextOptionsBuilder<DocumentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new DocumentDbContext(options);
        var now = DateTimeOffset.UtcNow;
        var tagNamespace = TagNamespace.Create("global", null, null, "System", isSystem: true, createdAtUtc: now);
        context.TagNamespaces.Add(tagNamespace);

        var tagLabel = TagLabel.Create(
            tagNamespace.Id,
            parentId: null,
            parentPathIds: [],
            name: "reviewed",
            sortOrder: 0,
            color: null,
            iconKey: null,
            createdBy: Guid.NewGuid(),
            isSystem: false,
            createdAtUtc: now);
        tagLabel.ClearDomainEvents();
        context.TagLabels.Add(tagLabel);
        await context.SaveChangesAsync();
        context.OutboxMessages.RemoveRange(context.OutboxMessages);
        await context.SaveChangesAsync();

        var repository = new TagLabelRepository(context);
        tagLabel.MarkDeleted(DateTimeOffset.UtcNow);

        await repository.RemoveAsync(tagLabel, CancellationToken.None);

        var message = Assert.Single(context.OutboxMessages);
        Assert.Equal(DocumentEventNames.TagLabelDeleted, message.Type);
    }
}
