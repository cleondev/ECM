using ECM.Document.Domain.Documents;
using ECM.Document.Domain.Documents.Events;
using ECM.Document.Infrastructure.Documents;
using ECM.Document.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Documents;
using Xunit;

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
        var document = Document.Create(
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
        Assert.Equal(nameof(DocumentTagAssignedContract), message.Type);
    }
}
