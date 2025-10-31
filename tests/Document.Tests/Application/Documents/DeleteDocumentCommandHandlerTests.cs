using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Document.Application.Documents.Commands;
using ECM.Document.Application.Documents.Repositories;
using ECM.Document.Domain.Documents;
using ECM.Document.Domain.Documents.Events;
using DomainDocument = ECM.Document.Domain.Documents.Document;
using TestFixtures;
using Xunit;

namespace Document.Tests.Application.Documents;

public sealed class DeleteDocumentCommandHandlerTests
{
    private readonly DefaultGroupFixture _groups = new();

    [Fact]
    public async Task HandleAsync_WhenDocumentExists_DeletesAndMarksOutbox()
    {
        var document = CreateSampleDocument();
        var repository = new FakeDocumentRepository(document);
        var clock = new FixedClock(new DateTimeOffset(2024, 05, 01, 0, 0, 0, TimeSpan.Zero));
        var handler = new DeleteDocumentCommandHandler(repository, clock);

        var command = new DeleteDocumentCommand(document.Id.Value, Guid.NewGuid());
        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repository.DeleteCalled);
        Assert.Equal(CancellationToken.None, repository.CapturedToken);
        Assert.Contains(document.DomainEvents, @event => @event is DocumentDeletedDomainEvent);
    }

    [Fact]
    public async Task HandleAsync_WhenDocumentIsMissing_ReturnsNotFound()
    {
        var repository = new FakeDocumentRepository(document: null);
        var handler = new DeleteDocumentCommandHandler(repository, new FixedClock(DateTimeOffset.UtcNow));

        var command = new DeleteDocumentCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Equals("Document not found.", StringComparison.Ordinal));
        Assert.False(repository.DeleteCalled);
    }

    [Fact]
    public async Task HandleAsync_WhenDeletedByIsMissing_ReturnsFailure()
    {
        var repository = new FakeDocumentRepository(CreateSampleDocument());
        var handler = new DeleteDocumentCommandHandler(repository, new FixedClock(DateTimeOffset.UtcNow));

        var command = new DeleteDocumentCommand(repository.Document!.Id.Value, Guid.Empty);
        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Equals("Deleted by is required.", StringComparison.Ordinal));
        Assert.False(repository.DeleteCalled);
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

        public bool DeleteCalled { get; private set; }

        public CancellationToken? CapturedToken { get; private set; }

        public Task<DomainDocument> AddAsync(DomainDocument document, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<DomainDocument?> GetAsync(DocumentId documentId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Document);
        }

        public Task DeleteAsync(DomainDocument document, CancellationToken cancellationToken = default)
        {
            DeleteCalled = true;
            CapturedToken = cancellationToken;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class FixedClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }
}
