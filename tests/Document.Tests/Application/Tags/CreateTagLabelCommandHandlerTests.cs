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
        var creatorId = Guid.NewGuid();
        var tagNamespace = TagNamespace.Create("user", creatorId, null, "My Tags", isSystem: false, createdAtUtc: now);
        var namespaceRepository = new FakeTagNamespaceRepository([tagNamespace]);
        var repository = new FakeTagLabelRepository();
        var clock = new FixedClock(now);
        var handler = new CreateTagLabelCommandHandler(repository, namespaceRepository, clock);

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
    public async Task HandleAsync_WhenNamespaceMissing_CreatesNamespace()
    {
        var now = new DateTimeOffset(2024, 02, 13, 12, 30, 0, TimeSpan.Zero);
        var repository = new FakeTagLabelRepository();
        var namespaceRepository = new FakeTagNamespaceRepository();
        var clock = new FixedClock(now);
        var handler = new CreateTagLabelCommandHandler(repository, namespaceRepository, clock);

        var creatorId = Guid.NewGuid();
        var command = new CreateTagLabelCommand(null, null, "Tag", null, null, null, creatorId, false);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var tag = Assert.IsType<TagLabelResult>(result.Value);
        Assert.NotEqual(Guid.Empty, tag.NamespaceId);
        Assert.Equal("Tag", tag.Name);

        var createdNamespace = namespaceRepository.StoredNamespaces.Single();
        Assert.Equal(tag.NamespaceId, createdNamespace.Id);
        Assert.Equal("user", createdNamespace.Scope, StringComparer.Ordinal);
        Assert.Equal(creatorId, createdNamespace.OwnerUserId);
        Assert.Equal("Personal Tags", createdNamespace.DisplayName);
        Assert.Single(repository.StoredTags);
    }

    [Fact]
    public async Task HandleAsync_WhenDuplicateNameExists_ReturnsFailure()
    {
        var now = DateTimeOffset.UtcNow;
        var creatorId = Guid.NewGuid();
        var tagNamespace = TagNamespace.Create("user", creatorId, null, "My Tags", isSystem: false, createdAtUtc: now);
        var namespaceRepository = new FakeTagNamespaceRepository([tagNamespace]);
        var repository = new FakeTagLabelRepository();
        var clock = new FixedClock(now);
        var handler = new CreateTagLabelCommandHandler(repository, namespaceRepository, clock);

        var existing = TagLabel.Create(tagNamespace.Id, null, [], "Duplicate", 0, null, null, creatorId, false, now);
        repository.Seed(existing);

        var command = new CreateTagLabelCommand(tagNamespace.Id, null, "Duplicate", null, null, null, creatorId, false);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("already exists", string.Join(' ', result.Errors), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleAsync_WhenNamespaceIsGlobal_UsesPersonalNamespace()
    {
        var now = DateTimeOffset.UtcNow;
        var creatorId = Guid.NewGuid();
        var tagNamespace = TagNamespace.Create("global", null, null, "Global", isSystem: true, createdAtUtc: now);
        var namespaceRepository = new FakeTagNamespaceRepository([tagNamespace]);
        var repository = new FakeTagLabelRepository();
        var clock = new FixedClock(now);
        var handler = new CreateTagLabelCommandHandler(repository, namespaceRepository, clock);

        var command = new CreateTagLabelCommand(tagNamespace.Id, null, "Personal", null, null, null, creatorId, false);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var tag = Assert.IsType<TagLabelResult>(result.Value);
        Assert.NotEqual(tagNamespace.Id, tag.NamespaceId);

        var personalNamespace = namespaceRepository.StoredNamespaces.Single(ns => ns.Scope == "user");
        Assert.Equal(creatorId, personalNamespace.OwnerUserId);
        Assert.Equal(tag.NamespaceId, personalNamespace.Id);
        Assert.Single(repository.StoredTags);
    }

    private sealed class FixedClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }
}
