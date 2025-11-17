using System;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Tagger;
using Xunit;

namespace Tagger.Tests;

public class TaggingEventProcessorTests
{
    [Fact]
    public async Task HandleDocumentUploadedAsync_AssignsMatchingTags()
    {
        var tagIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var ruleEngine = new RecordingRuleEngine(tagIds);
        var assignmentService = new RecordingAssignmentService();
        var processor = new TaggingEventProcessor(ruleEngine, assignmentService, NullLogger<TaggingEventProcessor>.Instance);

        var @event = new DocumentUploadedIntegrationEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            "Quarterly Plan",
            "Q1 plan",
            "Detailed content",
            new Dictionary<string, string> { ["extension"] = ".pdf" },
            null);

        await processor.HandleDocumentUploadedAsync(@event);

        Assert.Equal(@event.DocumentId, assignmentService.LastDocumentId);
        Assert.Equal(tagIds, assignmentService.LastTagIds);
        Assert.Equal(TaggingRuleTrigger.DocumentUploaded, ruleEngine.LastTrigger);
        Assert.NotNull(ruleEngine.LastContext);
        Assert.Equal("Quarterly Plan", ruleEngine.LastContext!.Title);
    }

    [Fact]
    public async Task HandleDocumentUploadedAsync_IgnoresWhenNoRulesMatch()
    {
        var ruleEngine = new RecordingRuleEngine(Array.Empty<Guid>());
        var assignmentService = new RecordingAssignmentService();
        var processor = new TaggingEventProcessor(ruleEngine, assignmentService, NullLogger<TaggingEventProcessor>.Instance);

        var @event = new DocumentUploadedIntegrationEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            "Misc",
            null,
            null,
            null,
            null);

        await processor.HandleDocumentUploadedAsync(@event);

        Assert.Equal(0, assignmentService.InvocationCount);
        Assert.Null(assignmentService.LastDocumentId);
    }

    [Fact]
    public async Task HandleOcrCompletedAsync_PropagatesTrigger()
    {
        var tags = new[] { Guid.NewGuid() };
        var ruleEngine = new RecordingRuleEngine(tags);
        var assignmentService = new RecordingAssignmentService();
        var processor = new TaggingEventProcessor(ruleEngine, assignmentService, NullLogger<TaggingEventProcessor>.Instance);

        var @event = new OcrCompletedIntegrationEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            "Scanned",
            "OCR summary",
            "OCR body",
            new Dictionary<string, string> { ["extension"] = ".tiff" },
            null);

        await processor.HandleOcrCompletedAsync(@event);

        Assert.Equal(TaggingRuleTrigger.OcrCompleted, ruleEngine.LastTrigger);
        Assert.Equal(1, assignmentService.InvocationCount);
    }
}
