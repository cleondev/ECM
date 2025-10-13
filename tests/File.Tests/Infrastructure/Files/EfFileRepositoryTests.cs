using ECM.File.Application.Files;
using ECM.File.Domain.Files;
using ECM.File.Infrastructure.Files;
using ECM.File.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Files;
using Xunit;

namespace File.Tests.Infrastructure.Files;

public class EfFileRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsOutboxMessageForUploadedFile()
    {
        var options = new DbContextOptionsBuilder<FileDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new FileDbContext(options);
        var repository = new EfFileRepository(context);

        var storedFile = StoredFile.Create("files/1.pdf", legalHold: false, DateTimeOffset.UtcNow);

        await repository.AddAsync(storedFile, CancellationToken.None);

        var message = Assert.Single(context.OutboxMessages);
        Assert.Equal("file", message.Aggregate);
        Assert.Equal(FileEventNames.StoredFileUploaded, message.Type);
    }
}
