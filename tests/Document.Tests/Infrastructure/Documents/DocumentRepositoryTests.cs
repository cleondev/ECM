using System;
using ECM.Document.Domain.Documents;
using ECM.Document.Domain.Documents.Events;
using ECM.Document.Infrastructure.Documents;
using ECM.Document.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Messaging;
using Xunit;

using DomainDocument = ECM.Document.Domain.Documents.Document;

namespace Document.Tests.Infrastructure.Documents;

public class DocumentRepositoryTests
{
    [Fact]
    public async Task SaveChangesAsync_PersistsOutboxMessageForAssignedTag()
    {
        var options = new DbContextOptionsBuilder<DocumentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new DocumentDbContext(options);
        var repository = new DocumentRepository(context);
        var now = DateTimeOffset.UtcNow;
        var document = DomainDocument.Create(
            DocumentTitle.Create("Project Charter"),
            "proposal",
            "draft",
            Guid.NewGuid(),
            Guid.NewGuid(),
            now);

        await repository.AddAsync(document, CancellationToken.None);
        context.OutboxMessages.RemoveRange(context.OutboxMessages);
        await context.SaveChangesAsync();

        var tagId = Guid.NewGuid();
        document.AssignTag(tagId, Guid.NewGuid(), now.AddMinutes(1));

        await repository.SaveChangesAsync(CancellationToken.None);

        var message = Assert.Single(context.OutboxMessages);
        Assert.Equal("document", message.Aggregate);
        Assert.Equal(EventNames.Document.TagAssigned, message.Type);
    }

    [Fact]
    public async Task SaveChangesAsync_PersistsOutboxMessageForUpdatedDocument()
    {
        var options = new DbContextOptionsBuilder<DocumentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new DocumentDbContext(options);
        var repository = new DocumentRepository(context);

        var now = DateTimeOffset.UtcNow;
        var document = DomainDocument.Create(
            DocumentTitle.Create("Project Charter"),
            "proposal",
            "draft",
            Guid.NewGuid(),
            Guid.NewGuid(),
            now);

        await repository.AddAsync(document, CancellationToken.None);
        context.OutboxMessages.RemoveRange(context.OutboxMessages);
        await context.SaveChangesAsync();

        document.UpdateStatus("published", now.AddMinutes(1));
        var updatedBy = Guid.NewGuid();
        var updatedAt = now.AddMinutes(1);
        document.MarkUpdated(updatedBy, updatedAt);

        await repository.SaveChangesAsync(CancellationToken.None);

        var message = Assert.Single(context.OutboxMessages);
        Assert.Equal("document", message.Aggregate);
        Assert.Equal(EventNames.Document.Updated, message.Type);
    }

    [Fact]
    public async Task DeleteAsync_PersistsOutboxMessageForDeletedDocument()
    {
        var options = new DbContextOptionsBuilder<DocumentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new DocumentDbContext(options);
        var repository = new DocumentRepository(context);

        var now = DateTimeOffset.UtcNow;
        var document = DomainDocument.Create(
            DocumentTitle.Create("Project Charter"),
            "proposal",
            "draft",
            Guid.NewGuid(),
            Guid.NewGuid(),
            now);

        await repository.AddAsync(document, CancellationToken.None);
        context.OutboxMessages.RemoveRange(context.OutboxMessages);
        await context.SaveChangesAsync();

        var deletedBy = Guid.NewGuid();
        var deletedAt = now.AddMinutes(5);
        document.MarkDeleted(deletedBy, deletedAt);

        await repository.DeleteAsync(document, CancellationToken.None);

        var message = Assert.Single(context.OutboxMessages);
        Assert.Equal("document", message.Aggregate);
        Assert.Equal(EventNames.Document.Deleted, message.Type);
}
}
