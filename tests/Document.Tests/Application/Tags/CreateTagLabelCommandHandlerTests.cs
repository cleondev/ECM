using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Document.Application.Tags.Commands;
using ECM.Document.Application.Tags.Results;
using Xunit;

namespace Document.Tests.Application.Tags;

public class CreateTagLabelCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithHierarchicalPath_AllowsForwardSlashes()
    {
        var now = new DateTimeOffset(2024, 02, 12, 9, 15, 0, TimeSpan.Zero);
        var repository = new FakeTagLabelRepository(["system"]);
        var clock = new FixedClock(now);
        var handler = new CreateTagLabelCommandHandler(repository, clock);

        var command = new CreateTagLabelCommand("System", "Operations", "Projects/Quarter-1", Guid.NewGuid());

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var tag = Assert.IsType<TagLabelResult>(result.Value);
        Assert.Equal("system", tag.NamespaceSlug);
        Assert.Equal("operations", tag.Slug);
        Assert.Equal("projects/quarter-1", tag.Path);

        var storedTag = Assert.Single(repository.StoredTags);
        Assert.Equal(tag.Id, storedTag.Id);
        Assert.Equal("projects/quarter-1", storedTag.Path);
        Assert.Equal(CancellationToken.None, repository.CapturedToken);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidCharactersInPath_ReturnsFailure()
    {
        var repository = new FakeTagLabelRepository(["system"]);
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var handler = new CreateTagLabelCommandHandler(repository, clock);

        var command = new CreateTagLabelCommand("system", "ops", "invalid#path", Guid.NewGuid());

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("forward slashes", result.Errors.Single());
        Assert.Empty(repository.StoredTags);
        Assert.Null(repository.CapturedToken);
    }

    private sealed class FixedClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }
}
