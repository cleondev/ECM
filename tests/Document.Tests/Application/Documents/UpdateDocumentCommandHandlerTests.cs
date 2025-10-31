using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Document.Application.Documents.Commands;
using ECM.Document.Application.Documents.Repositories;
using ECM.Document.Domain.Documents;
using DomainDocument = ECM.Document.Domain.Documents.Document;
using TestFixtures;
using Xunit;

namespace Document.Tests.Application.Documents;

public sealed class UpdateDocumentCommandHandlerTests
{
    private readonly DefaultGroupFixture _groups = new();

    [Fact]
    public async Task HandleAsync_WithValidPayload_UpdatesDocumentAndPersistsChanges()
    {
        var now = new DateTimeOffset(2024, 03, 18, 7, 45, 0, TimeSpan.Zero);
        var repository = new FakeDocumentRepository(CreateSampleDocument());
        var clock = new FixedClock(now);
        var handler = new UpdateDocumentCommandHandler(repository, clock);

        var groupId = Guid.NewGuid();
        var command = new UpdateDocumentCommand(
            repository.Document!.Id.Value,
            UpdatedBy: Guid.NewGuid(),
            "  Updated Title  ",
            "  Reviewed  ",
            "  Confidential  ",
            HasGroupId: true,
            GroupId: groupId);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var document = result.Value;
        Assert.Same(repository.Document, document);
        Assert.Equal("Updated Title", document!.Title);
        Assert.Equal("Reviewed", document.Status);
        Assert.Equal("Confidential", document.Sensitivity);
        Assert.Equal(groupId, document.GroupId);
        Assert.Equal(now, document.UpdatedAtUtc);
        Assert.True(repository.SaveChangesCalled);
        Assert.Equal(CancellationToken.None, repository.CapturedToken);
    }

    [Fact]
    public async Task HandleAsync_WhenDocumentDoesNotExist_ReturnsNotFound()
    {
        var repository = new FakeDocumentRepository(document: null);
        var handler = new UpdateDocumentCommandHandler(repository, new FixedClock(DateTimeOffset.UtcNow));

        var command = new UpdateDocumentCommand(
            Guid.NewGuid(),
            UpdatedBy: Guid.NewGuid(),
            Title: "Quarterly Report",
            Status: null,
            Sensitivity: null,
            HasGroupId: false,
            GroupId: null);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.True(UpdateDocumentCommandHandler.IsNotFound(result));
        Assert.False(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidTitle_ReturnsFailureAndSkipsSave()
    {
        var repository = new FakeDocumentRepository(CreateSampleDocument());
        var handler = new UpdateDocumentCommandHandler(repository, new FixedClock(DateTimeOffset.UtcNow));

        var command = new UpdateDocumentCommand(
            repository.Document!.Id.Value,
            UpdatedBy: Guid.NewGuid(),
            Title: "   ",
            Status: null,
            Sensitivity: null,
            HasGroupId: false,
            GroupId: null);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.StartsWith("Document title is required.", StringComparison.Ordinal));
        Assert.False(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task HandleAsync_WhenUpdatedByIsMissing_ReturnsFailure()
    {
        var repository = new FakeDocumentRepository(CreateSampleDocument());
        var handler = new UpdateDocumentCommandHandler(repository, new FixedClock(DateTimeOffset.UtcNow));

        var command = new UpdateDocumentCommand(
            repository.Document!.Id.Value,
            UpdatedBy: Guid.Empty,
            Title: "Updated",
            Status: null,
            Sensitivity: null,
            HasGroupId: false,
            GroupId: null);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Equals("Updated by is required.", StringComparison.Ordinal));
        Assert.False(repository.SaveChangesCalled);
    }

    private DomainDocument CreateSampleDocument()
    {
        var now = new DateTimeOffset(2024, 03, 10, 9, 0, 0, TimeSpan.Zero);
        return DomainDocument.Create(
            "Employee Handbook",
            "Policy",
            "Draft",
            _groups.SystemGroupId,
            _groups.SystemGroupId,
            now,
            _groups.SystemGroupId,
            "Internal",
            typeId: null);
    }

    private sealed class FakeDocumentRepository(DomainDocument? document) : IDocumentRepository
    {
        public DomainDocument? Document { get; private set; } = document;

        public bool SaveChangesCalled { get; private set; }

        public CancellationToken? CapturedToken { get; private set; }

        public Task<DomainDocument> AddAsync(
            DomainDocument document,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<DomainDocument?> GetAsync(
            DocumentId documentId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Document);
        }

        public Task DeleteAsync(
            DomainDocument document,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalled = true;
            CapturedToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }
}
