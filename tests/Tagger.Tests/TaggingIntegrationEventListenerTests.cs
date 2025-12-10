using System.Text.Json;

using Ecm.Rules.Abstractions;
using Ecm.Rules.Engine;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Shared.Contracts.Messaging;

using Tagger.Processing;
using Tagger.Rules.Configuration;
using Tagger.Services;

using Workers.Shared.Messaging;

using Xunit;

namespace Tagger.Tests;

public class TaggingIntegrationEventListenerTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task HandleDocumentUploadedAsync_DeserializesEventAndAssignsTags()
    {
        var documentId = Guid.NewGuid();
        var ruleEngine = new RecordingRuleEngine(new[] { Guid.NewGuid() });
        var assignmentService = new RecordingAssignmentService();

        using var provider = BuildServiceProvider(ruleEngine, assignmentService);
        var listener = new TaggingIntegrationEventListener(
            new NoOpKafkaConsumer(NullLogger<NoOpKafkaConsumer>.Instance),
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<TaggingIntegrationEventListener>.Instance);

        var groupIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var payload = JsonSerializer.Serialize(new
        {
            eventId = Guid.NewGuid(),
            occurredAtUtc = DateTimeOffset.UtcNow,
            data = new
            {
                documentId,
                title = "Employee Handbook",
                summary = "Updated policies",
                content = "Full text",
                metadata = new Dictionary<string, object>
                {
                    ["extension"] = ".pdf",
                    ["groupIds"] = groupIds,
                    ["department"] = "HR"
                },
                tags = new[] { "hr" },
                groupIds
            }
        }, SerializerOptions);

        var message = new KafkaMessage(
            EventNames.Document.Uploaded,
            documentId.ToString(),
            payload,
            DateTimeOffset.UtcNow);

        await listener.HandleDocumentUploadedAsync(message, CancellationToken.None);

        Assert.Equal(documentId, assignmentService.LastDocumentId);
        Assert.NotNull(ruleEngine.LastContext);
        Assert.True(ruleEngine.LastContext!.Has("groupIds"));
        Assert.False(ruleEngine.LastContext.Has("department"));
        Assert.Equal(TaggingRuleSetNames.DocumentUploaded, ruleEngine.LastRuleSet);
    }

    [Fact]
    public async Task HandleOcrCompletedAsync_UsesOcrRuleSet()
    {
        var documentId = Guid.NewGuid();
        var ruleEngine = new RecordingRuleEngine(new[] { Guid.NewGuid() });
        var assignmentService = new RecordingAssignmentService();

        using var provider = BuildServiceProvider(ruleEngine, assignmentService);
        var listener = new TaggingIntegrationEventListener(
            new NoOpKafkaConsumer(NullLogger<NoOpKafkaConsumer>.Instance),
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<TaggingIntegrationEventListener>.Instance);

        var payload = JsonSerializer.Serialize(new
        {
            eventId = Guid.NewGuid(),
            occurredAtUtc = DateTimeOffset.UtcNow,
            data = new
            {
                documentId,
                title = "Scanned Contract",
                summary = "OCR summary",
                content = "OCR content",
                metadata = new Dictionary<string, object>
                {
                    ["extension"] = ".tiff",
                    ["source"] = "ocr"
                },
                tags = new[] { "legal" },
                groupIds = new[] { Guid.NewGuid() }
            }
        }, SerializerOptions);

        var message = new KafkaMessage(
            EventNames.Ocr.Completed,
            documentId.ToString(),
            payload,
            DateTimeOffset.UtcNow);

        await listener.HandleOcrCompletedAsync(message, CancellationToken.None);

        Assert.Equal(TaggingRuleSetNames.OcrCompleted, ruleEngine.LastRuleSet);
        Assert.Equal(1, assignmentService.InvocationCount);
    }

    private static ServiceProvider BuildServiceProvider(
        RecordingRuleEngine ruleEngine,
        RecordingAssignmentService assignmentService)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<TaggingEventProcessor>();
        services.AddSingleton<IRuleEngine>(_ => ruleEngine);
        services.AddSingleton<IDocumentTagAssignmentService>(_ => assignmentService);
        services.AddSingleton<IRuleContextFactory, RuleContextFactory>();
        services.AddSingleton<ITaggingRuleContextFactory, TaggingRuleContextFactory>();
        return services.BuildServiceProvider();
    }
}
