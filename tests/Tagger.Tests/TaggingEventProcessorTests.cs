using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ecm.Rules.Engine;
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
        var contextFactory = new TaggingRuleContextFactory(new RuleContextFactory(), Array.Empty<ITaggingRuleContextEnricher>());
        var processor = new TaggingEventProcessor(
            ruleEngine,
            assignmentService,
            contextFactory,
            NullLogger<TaggingEventProcessor>.Instance);

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
        Assert.Equal(TaggingRuleSetNames.DocumentUploaded, ruleEngine.LastRuleSet);
        Assert.NotNull(ruleEngine.LastContext);
        Assert.Equal("Quarterly Plan", ruleEngine.LastContext!.Get<string>("Title"));
    }

    [Fact]
    public async Task HandleDocumentUploadedAsync_IgnoresWhenNoRulesMatch()
    {
        var ruleEngine = new RecordingRuleEngine(Array.Empty<Guid>());
        var assignmentService = new RecordingAssignmentService();
        var contextFactory = new TaggingRuleContextFactory(new RuleContextFactory(), Array.Empty<ITaggingRuleContextEnricher>());
        var processor = new TaggingEventProcessor(
            ruleEngine,
            assignmentService,
            contextFactory,
            NullLogger<TaggingEventProcessor>.Instance);

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
        var contextFactory = new TaggingRuleContextFactory(new RuleContextFactory(), Array.Empty<ITaggingRuleContextEnricher>());
        var processor = new TaggingEventProcessor(
            ruleEngine,
            assignmentService,
            contextFactory,
            NullLogger<TaggingEventProcessor>.Instance);

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

        Assert.Equal(TaggingRuleSetNames.OcrCompleted, ruleEngine.LastRuleSet);
        Assert.Equal(1, assignmentService.InvocationCount);
    }

    [Fact]
    public async Task HandleDocumentUploadedAsync_EnrichesContext()
    {
        var ruleEngine = new RecordingRuleEngine(new[] { Guid.NewGuid() });
        var assignmentService = new RecordingAssignmentService();
        var enricher = new RecordingContextEnricher();
        var contextFactory = new TaggingRuleContextFactory(new RuleContextFactory(), new[] { enricher });
        var processor = new TaggingEventProcessor(
            ruleEngine,
            assignmentService,
            contextFactory,
            NullLogger<TaggingEventProcessor>.Instance);

        var @event = new DocumentUploadedIntegrationEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            "Quarterly Plan",
            "Q1 plan",
            "Detailed content",
            new Dictionary<string, string>(),
            null);

        await processor.HandleDocumentUploadedAsync(@event);

        Assert.Equal(@event.DocumentId, enricher.LastDocumentId);
    }

    private sealed class RecordingContextEnricher : ITaggingRuleContextEnricher
    {
        public Guid? LastDocumentId { get; private set; }

        public void Enrich(TaggingRuleContextBuilder builder, ITaggingIntegrationEvent integrationEvent)
        {
            LastDocumentId = integrationEvent.DocumentId;
            builder.AddField("custom", integrationEvent.DocumentId.ToString());
        }
    }
}
