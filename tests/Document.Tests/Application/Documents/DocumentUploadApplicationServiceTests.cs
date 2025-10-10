using ECM.Abstractions.Files;
using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Document.Application.Documents;
using ECM.Document.Domain.Documents;

namespace Document.Tests.Application.Documents;

public class DocumentUploadApplicationServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenUploadSucceeds_PersistsDocumentAndReturnsSummary()
    {
        var now = DateTimeOffset.UtcNow;
        var repository = new FakeDocumentRepository();
        var fileUploadService = new FakeFileUploadService
        {
            Result = OperationResult<FileUploadResult>.Success(new FileUploadResult("storage-key", "file.pdf", "application/pdf", 3, now))
        };
        var clock = new FixedClock(now);
        var service = new DocumentUploadApplicationService(repository, fileUploadService, clock);

        await using var content = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadDocumentCommand(
            "Document",
            "Policy",
            "Draft",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Operations",
            "Internal",
            Guid.NewGuid(),
            "file.pdf",
            "application/pdf",
            content.Length,
            new string('a', 64),
            content);

        var result = await service.CreateAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(repository.Documents);
        Assert.Equal("storage-key", result.Value!.LatestVersion!.StorageKey);
        Assert.Equal("application/pdf", result.Value.LatestVersion.MimeType);
        Assert.Equal(1, result.Value.LatestVersion.VersionNo);
    }

    [Fact]
    public async Task CreateAsync_WhenUploadFails_ReturnsFailure()
    {
        var now = DateTimeOffset.UtcNow;
        var repository = new FakeDocumentRepository();
        var fileUploadService = new FakeFileUploadService
        {
            Result = OperationResult<FileUploadResult>.Failure("upload failed")
        };
        var clock = new FixedClock(now);
        var service = new DocumentUploadApplicationService(repository, fileUploadService, clock);

        await using var content = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadDocumentCommand(
            "Document",
            "Policy",
            "Draft",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            null,
            "file.pdf",
            "application/pdf",
            content.Length,
            new string('a', 64),
            content);

        var result = await service.CreateAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(repository.Documents);
    }

    private sealed class FakeDocumentRepository : IDocumentRepository
    {
        public List<Document> Documents { get; } = [];

        public Task<Document> AddAsync(Document document, CancellationToken cancellationToken = default)
        {
            Documents.Add(document);
            return Task.FromResult(document);
        }

        public Task<Document?> GetAsync(DocumentId documentId, CancellationToken cancellationToken = default)
            => Task.FromResult<Document?>(null);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeFileUploadService : IFileUploadService
    {
        public OperationResult<FileUploadResult>? Result { get; init; }

        public Task<OperationResult<FileUploadResult>> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result ?? OperationResult<FileUploadResult>.Failure("No result configured."));
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }
}
