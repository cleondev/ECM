using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ECM.Abstractions.Files;
using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Document.Application.Documents.AccessControl;
using ECM.Document.Application.Documents.Commands;
using ECM.Document.Application.Documents.Repositories;
using ECM.Document.Domain.Documents;
using DomainDocument = ECM.Document.Domain.Documents.Document;
using TestFixtures;
using Xunit;

namespace Document.Tests.Application.Documents;

public class UploadDocumentCommandHandlerTests
{
    private readonly DefaultGroupFixture _groups = new();

    [Fact]
    public async Task CreateAsync_WhenUploadSucceeds_PersistsDocumentAndReturnsSummary()
    {
        var now = DateTimeOffset.UtcNow;
        var repository = new FakeDocumentRepository();
        var fileStorageGateway = new FakeFileStorageGateway
        {
            Result = OperationResult<FileUploadResult>.Success(new FileUploadResult("storage-key", "file.pdf", "application/pdf", 3, now))
        };
        var clock = new FixedClock(now);
        var aclWriter = new FakeEffectiveAclFlatWriter();
        var handler = new UploadDocumentCommandHandler(repository, fileStorageGateway, clock, aclWriter);

        await using var content = new MemoryStream([1, 2, 3]);
        var command = new UploadDocumentCommand(
            "Document",
            "Policy",
            "Draft",
            _groups.GuestGroupId,
            _groups.SystemGroupId,
            _groups.GuestGroupId,
            "Internal",
            Guid.NewGuid(),
            "file.pdf",
            "application/pdf",
            content.Length,
            new string('a', 64),
            content);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(repository.Documents);
        Assert.Collection(
            aclWriter.Entries,
            entry =>
            {
                Assert.Equal(repository.Documents[0].Id, entry.DocumentId);
                Assert.Equal(repository.Documents[0].OwnerId, entry.UserId);
                Assert.Equal(EffectiveAclFlatSources.Owner, entry.Source);
                Assert.Equal(EffectiveAclFlatIdempotencyKeys.Owner, entry.IdempotencyKey);
            });
        Assert.Equal("storage-key", result.Value!.LatestVersion!.StorageKey);
        Assert.Equal("application/pdf", result.Value.LatestVersion.MimeType);
        Assert.Equal(1, result.Value.LatestVersion.VersionNo);
        Assert.Equal([_groups.GuestGroupId], result.Value.GroupIds);
    }

    [Fact]
    public async Task CreateAsync_WhenUploadFails_ReturnsFailure()
    {
        var now = DateTimeOffset.UtcNow;
        var repository = new FakeDocumentRepository();
        var fileStorageGateway = new FakeFileStorageGateway
        {
            Result = OperationResult<FileUploadResult>.Failure("upload failed")
        };
        var clock = new FixedClock(now);
        var handler = new UploadDocumentCommandHandler(repository, fileStorageGateway, clock, new FakeEffectiveAclFlatWriter());

        await using var content = new MemoryStream([1, 2, 3]);
        var command = new UploadDocumentCommand(
            "Document",
            "Policy",
            "Draft",
            _groups.GuestGroupId,
            _groups.SystemGroupId,
            null,
            null,
            null,
            "file.pdf",
            "application/pdf",
            content.Length,
            new string('a', 64),
            content);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(repository.Documents);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateAsync_WithNonPositiveFileSize_ReturnsFailureWithoutUploading(long fileSize)
    {
        var now = DateTimeOffset.UtcNow;
        var repository = new FakeDocumentRepository();
        var fileStorageGateway = new FakeFileStorageGateway();
        var clock = new FixedClock(now);
        var handler = new UploadDocumentCommandHandler(repository, fileStorageGateway, clock, new FakeEffectiveAclFlatWriter());

        await using var content = new MemoryStream([1, 2, 3]);
        var command = new UploadDocumentCommand(
            "Document",
            "Policy",
            "Draft",
            _groups.GuestGroupId,
            _groups.SystemGroupId,
            _groups.GuestGroupId,
            "Internal",
            Guid.NewGuid(),
            "file.pdf",
            "application/pdf",
            fileSize,
            new string('a', 64),
            content);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(repository.Documents);
        Assert.Empty(fileStorageGateway.Requests);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidTitle_ReturnsFailureWithoutUploading()
    {
        var now = DateTimeOffset.UtcNow;
        var repository = new FakeDocumentRepository();
        var fileStorageGateway = new FakeFileStorageGateway();
        var clock = new FixedClock(now);
        var handler = new UploadDocumentCommandHandler(repository, fileStorageGateway, clock, new FakeEffectiveAclFlatWriter());

        await using var content = new MemoryStream([1, 2, 3]);
        var command = new UploadDocumentCommand(
            "   ",
            "Policy",
            "Draft",
            _groups.GuestGroupId,
            _groups.SystemGroupId,
            _groups.GuestGroupId,
            "Internal",
            Guid.NewGuid(),
            "file.pdf",
            "application/pdf",
            content.Length,
            new string('a', 64),
            content);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(repository.Documents);
        Assert.Empty(fileStorageGateway.Requests);
    }

    [Fact]
    public async Task CreateAsync_WhenVersionCreationFails_ReturnsFailure()
    {
        var now = DateTimeOffset.UtcNow;
        var repository = new FakeDocumentRepository();
        var fileStorageGateway = new FakeFileStorageGateway
        {
            Result = OperationResult<FileUploadResult>.Success(new FileUploadResult("storage-key", "file.pdf", "application/pdf", 0, now))
        };
        var clock = new FixedClock(now);
        var handler = new UploadDocumentCommandHandler(repository, fileStorageGateway, clock, new FakeEffectiveAclFlatWriter());

        await using var content = new MemoryStream([1, 2, 3]);
        var command = new UploadDocumentCommand(
            "Document",
            "Policy",
            "Draft",
            _groups.GuestGroupId,
            _groups.SystemGroupId,
            _groups.GuestGroupId,
            "Internal",
            Guid.NewGuid(),
            "file.pdf",
            "application/pdf",
            content.Length,
            new string('a', 64),
            content);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(repository.Documents);
        Assert.Single(fileStorageGateway.Requests);
    }

    private sealed class FakeDocumentRepository : IDocumentRepository
    {
        public List<DomainDocument> Documents { get; } = [];

        public Task<DomainDocument> AddAsync(DomainDocument document, CancellationToken cancellationToken = default)
        {
            Documents.Add(document);
            return Task.FromResult(document);
        }

        public Task<DomainDocument?> GetAsync(DocumentId documentId, CancellationToken cancellationToken = default)
            => Task.FromResult<DomainDocument?>(null);

        public Task DeleteAsync(DomainDocument document, CancellationToken cancellationToken = default)
        {
            Documents.Remove(document);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeFileStorageGateway : IFileStorageGateway
    {
        public OperationResult<FileUploadResult>? Result { get; set; }

        public List<FileUploadRequest> Requests { get; } = [];

        public Task<OperationResult<FileUploadResult>> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(Result ?? OperationResult<FileUploadResult>.Failure("No result configured."));
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }

    private sealed class FakeEffectiveAclFlatWriter : IEffectiveAclFlatWriter
    {
        public List<EffectiveAclFlatWriteEntry> Entries { get; } = [];

        public Task UpsertAsync(EffectiveAclFlatWriteEntry entry, CancellationToken cancellationToken = default)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task UpsertAsync(IEnumerable<EffectiveAclFlatWriteEntry> entries, CancellationToken cancellationToken = default)
        {
            Entries.AddRange(entries);
            return Task.CompletedTask;
        }
    }
}
