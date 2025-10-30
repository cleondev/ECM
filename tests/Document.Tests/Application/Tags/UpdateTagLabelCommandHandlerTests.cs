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
    public async Task HandleAsync_WithHierarchicalPath_AllowsForwardSlashes()
    {
        var createdAt = new DateTimeOffset(2024, 02, 12, 9, 15, 0, TimeSpan.Zero);
        var existingTag = TagLabel.Create("system", "operations", "operations", Guid.NewGuid(), createdAt);
        var repository = new FakeTagLabelRepository(new[] { "system" });
        repository.Seed(existingTag);

        var clock = new FixedClock(createdAt.AddMinutes(5));
        var handler = new UpdateTagLabelCommandHandler(repository, clock);

        var command = new UpdateTagLabelCommand(
            existingTag.Id,
            "System",
            "Operations",
            "Operations/Site-A",
            Guid.NewGuid());

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var tag = Assert.IsType<TagLabelResult>(result.Value);
        Assert.Equal("operations", tag.Slug);
        Assert.Equal("operations/site-a", tag.Path);

        var storedTag = Assert.Single(repository.StoredTags);
        Assert.Equal(existingTag.Id, storedTag.Id);
        Assert.Equal("operations/site-a", storedTag.Path);
        Assert.Equal(CancellationToken.None, repository.CapturedToken);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidCharactersInPath_ReturnsFailure()
    {
        var createdAt = new DateTimeOffset(2024, 02, 12, 9, 15, 0, TimeSpan.Zero);
        var existingTag = TagLabel.Create("system", "ops", "ops", Guid.NewGuid(), createdAt);
        var repository = new FakeTagLabelRepository(new[] { "system" });
        repository.Seed(existingTag);

        var clock = new FixedClock(createdAt.AddMinutes(1));
        var handler = new UpdateTagLabelCommandHandler(repository, clock);

        var command = new UpdateTagLabelCommand(existingTag.Id, "system", "ops", "ops#invalid", Guid.NewGuid());

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("forward slashes", result.Errors.Single());

        var storedTag = Assert.Single(repository.StoredTags);
        Assert.Equal("ops", storedTag.Path);
        Assert.Null(repository.CapturedToken);
    }

    private sealed class FixedClock : ISystemClock
    {
        public FixedClock(DateTimeOffset now) => UtcNow = now;

        public DateTimeOffset UtcNow { get; }
    }
}
