using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Document.Application.Tags.Commands;
using ECM.Document.Application.Tags.Results;
using ECM.Document.Domain.Tags;
using Xunit;

namespace Document.Tests.Application.Tags;

public class CreateTagLabelCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithParent_ComputesPathIds()
    {
        var now = new DateTimeOffset(2024, 02, 12, 9, 15, 0, TimeSpan.Zero);
        var tagNamespace = TagNamespace.Create("user", Guid.NewGuid(), null, "My Tags", isSystem: false, createdAtUtc: now);
        var namespaceRepository = new FakeTagNamespaceRepository([tagNamespace]);
        var repository = new FakeTagLabelRepository();
        var clock = new FixedClock(now);
        var handler = new CreateTagLabelCommandHandler(repository, namespaceRepository, clock);

        var creatorId = Guid.NewGuid();
        var parent = TagLabel.Create(tagNamespace.Id, null, [], "Parent", 0, null, null, creatorId, false, now);
        repository.Seed(parent);

        var command = new CreateTagLabelCommand(
            tagNamespace.Id,
            parent.Id,
            "Child",
            5,
            "#FDE68A",
            "lucide:tag",
            creatorId,
            false);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var tag = Assert.IsType<TagLabelResult>(result.Value);
        Assert.Equal(tagNamespace.Id, tag.NamespaceId);
        Assert.Equal(parent.Id, tag.ParentId);
        Assert.Equal([parent.Id, tag.Id], tag.PathIds);
        Assert.Equal(5, tag.SortOrder);
        Assert.Equal("#FDE68A", tag.Color);
        Assert.Equal("lucide:tag", tag.IconKey);

        var storedTag = repository.StoredTags.Single(t => t.Id == tag.Id);
        Assert.Equal(tag.PathIds, storedTag.PathIds);
        Assert.Equal(CancellationToken.None, repository.CapturedToken);
    }

    [Fact]
    public async Task HandleAsync_WhenNamespaceMissing_ReturnsFailure()
    {
        var repository = new FakeTagLabelRepository();
        var namespaceRepository = new FakeTagNamespaceRepository();
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var handler = new CreateTagLabelCommandHandler(repository, namespaceRepository, clock);

        var command = new CreateTagLabelCommand(Guid.NewGuid(), null, "Tag", null, null, null, Guid.NewGuid(), false);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("namespace", string.Join(' ', result.Errors), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(repository.StoredTags);
    }

    [Fact]
    public async Task HandleAsync_WhenDuplicateNameExists_ReturnsFailure()
    {
        var now = DateTimeOffset.UtcNow;
        var tagNamespace = TagNamespace.Create("global", null, null, "Global", isSystem: true, createdAtUtc: now);
        var namespaceRepository = new FakeTagNamespaceRepository([tagNamespace]);
        var repository = new FakeTagLabelRepository();
        var clock = new FixedClock(now);
        var handler = new CreateTagLabelCommandHandler(repository, namespaceRepository, clock);

        var creatorId = Guid.NewGuid();
        var existing = TagLabel.Create(tagNamespace.Id, null, [], "Duplicate", 0, null, null, creatorId, false, now);
        repository.Seed(existing);

        var command = new CreateTagLabelCommand(tagNamespace.Id, null, "Duplicate", null, null, null, creatorId, false);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("already exists", string.Join(' ', result.Errors), StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FixedClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }
}
