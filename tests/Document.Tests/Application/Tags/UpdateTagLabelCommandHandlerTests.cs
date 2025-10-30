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

public class UpdateTagLabelCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenParentChanges_RecalculatesPath()
    {
        var now = new DateTimeOffset(2024, 02, 12, 9, 15, 0, TimeSpan.Zero);
        var tagNamespace = TagNamespace.Create("group", null, Guid.NewGuid(), "Team", isSystem: false, createdAtUtc: now);
        var namespaceRepository = new FakeTagNamespaceRepository([tagNamespace]);
        var repository = new FakeTagLabelRepository();
        var clock = new FixedClock(now.AddMinutes(10));
        var handler = new UpdateTagLabelCommandHandler(repository, namespaceRepository, clock);

        var ownerId = Guid.NewGuid();
        var originalParent = TagLabel.Create(tagNamespace.Id, null, [], "Root", 0, null, null, ownerId, false, now);
        var newParent = TagLabel.Create(tagNamespace.Id, null, [], "Folder", 1, null, null, ownerId, false, now);
        var tag = TagLabel.Create(tagNamespace.Id, originalParent.Id, originalParent.PathIds, "Leaf", 0, null, null, ownerId, false, now);

        repository.Seed(originalParent);
        repository.Seed(newParent);
        repository.Seed(tag);

        var command = new UpdateTagLabelCommand(
            tag.Id,
            tagNamespace.Id,
            newParent.Id,
            "Leaf",
            3,
            "#fff",
            "icon",
            true,
            ownerId);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var updated = Assert.IsType<TagLabelResult>(result.Value);
        Assert.Equal(newParent.Id, updated.ParentId);
        Assert.Equal([newParent.Id, tag.Id], updated.PathIds);
        Assert.Equal(3, updated.SortOrder);
        Assert.Equal("#fff", updated.Color);
        Assert.Equal("icon", updated.IconKey);

        var stored = repository.StoredTags.Single(t => t.Id == tag.Id);
        Assert.Equal(updated.PathIds, stored.PathIds);
        Assert.Equal(CancellationToken.None, repository.CapturedToken);
    }

    [Fact]
    public async Task HandleAsync_WhenAssigningDescendantParent_ReturnsFailure()
    {
        var now = DateTimeOffset.UtcNow;
        var tagNamespace = TagNamespace.Create("global", null, null, "Global", isSystem: false, createdAtUtc: now);
        var namespaceRepository = new FakeTagNamespaceRepository([tagNamespace]);
        var repository = new FakeTagLabelRepository();
        var clock = new FixedClock(now.AddMinutes(1));
        var handler = new UpdateTagLabelCommandHandler(repository, namespaceRepository, clock);

        var ownerId = Guid.NewGuid();
        var root = TagLabel.Create(tagNamespace.Id, null, [], "Root", 0, null, null, ownerId, false, now);
        var child = TagLabel.Create(tagNamespace.Id, root.Id, root.PathIds, "Child", 0, null, null, ownerId, false, now);

        repository.Seed(root);
        repository.Seed(child);

        var command = new UpdateTagLabelCommand(
            root.Id,
            tagNamespace.Id,
            child.Id,
            "Root",
            null,
            null,
            null,
            true,
            ownerId);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("descendant", string.Join(' ', result.Errors), StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FixedClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }
}
