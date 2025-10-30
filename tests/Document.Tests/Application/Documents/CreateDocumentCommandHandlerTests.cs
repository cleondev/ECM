using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Document.Application.Documents.AccessControl;
using ECM.Document.Application.Documents.Commands;
using ECM.Document.Application.Documents.Repositories;
using ECM.Document.Application.Documents.Summaries;
using ECM.Document.Domain.Documents;
using DomainDocument = ECM.Document.Domain.Documents.Document;
using TestFixtures;
using Xunit;

namespace Document.Tests.Application.Documents;

public class CreateDocumentCommandHandlerTests
{
    private readonly DefaultGroupFixture _groups = new();

    [Fact]
    public async Task HandleAsync_WithValidRequest_PersistsDocumentAndReturnsSummary()
    {
        var now = new DateTimeOffset(2024, 01, 15, 8, 30, 0, TimeSpan.Zero);
        var repository = new FakeDocumentRepository();
        var clock = new FixedClock(now);
        var aclWriter = new FakeEffectiveAclFlatWriter();
        var handler = new CreateDocumentCommandHandler(repository, clock, aclWriter);

        var ownerId = _groups.GuestGroupId;
        var createdBy = _groups.SystemGroupId;
        var documentTypeId = Guid.NewGuid();
        var command = new CreateDocumentCommand(
            "  Employee Handbook  ",
            "  Policy  ",
            "  Draft  ",
            ownerId,
            createdBy,
            _groups.GuestGroupId,
            "  Confidential  ",
            documentTypeId);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var summary = Assert.IsType<DocumentSummaryResult>(result.Value);
        Assert.Equal("Employee Handbook", summary.Title);
        Assert.Equal("Policy", summary.DocType);
        Assert.Equal("Draft", summary.Status);
        Assert.Equal("Confidential", summary.Sensitivity);
        Assert.Equal(ownerId, summary.OwnerId);
        Assert.Equal(createdBy, summary.CreatedBy);
        Assert.Equal(_groups.GuestGroupId, summary.GroupId);
        Assert.Equal([_groups.GuestGroupId], summary.GroupIds);
        Assert.Equal(now, summary.CreatedAtUtc);
        Assert.Equal(now, summary.UpdatedAtUtc);
        Assert.Equal(documentTypeId, summary.DocumentTypeId);

        var storedDocument = Assert.Single(repository.AddedDocuments);
        Assert.Equal(summary.Id, storedDocument.Id.Value);
        Assert.Equal(now, storedDocument.CreatedAtUtc);
        Assert.Equal(now, storedDocument.UpdatedAtUtc);
        Assert.Equal(CancellationToken.None, repository.CapturedToken);

        Assert.Collection(
            aclWriter.Entries,
            entry =>
            {
                Assert.Equal(storedDocument.Id.Value, entry.DocumentId);
                Assert.Equal(storedDocument.OwnerId, entry.UserId);
            });
    }

    [Fact]
    public async Task HandleAsync_WithInvalidTitle_ReturnsFailureWithoutPersisting()
    {
        var repository = new FakeDocumentRepository();
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var handler = new CreateDocumentCommandHandler(repository, clock, new FakeEffectiveAclFlatWriter());

        var command = new CreateDocumentCommand(
            "   ",
            "Policy",
            "Draft",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            null);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.StartsWith("Document title is required.", StringComparison.Ordinal));
        Assert.Empty(repository.AddedDocuments);
        Assert.Null(repository.CapturedToken);
    }

    [Fact]
    public async Task HandleAsync_WhenDocumentCreationFails_ReturnsFailure()
    {
        var repository = new FakeDocumentRepository();
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var handler = new CreateDocumentCommandHandler(repository, clock, new FakeEffectiveAclFlatWriter());

        var command = new CreateDocumentCommand(
            "Project Plan",
            "   ",
            "Draft",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            null);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.StartsWith("Document type is required.", StringComparison.Ordinal));
        Assert.Empty(repository.AddedDocuments);
        Assert.Null(repository.CapturedToken);
    }

    private sealed class FakeDocumentRepository : IDocumentRepository
    {
        public List<DomainDocument> AddedDocuments { get; } = [];

        public CancellationToken? CapturedToken { get; private set; }

        public Task<DomainDocument> AddAsync(DomainDocument document, CancellationToken cancellationToken = default)
        {
            AddedDocuments.Add(document);
            CapturedToken = cancellationToken;
            return Task.FromResult(document);
        }

        public Task<DomainDocument?> GetAsync(DocumentId documentId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeleteAsync(DomainDocument document, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
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
