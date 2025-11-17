using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Contracts.Messaging;
using Tagger;
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
            EventTopics.Pipelines.Document.Uploaded,
            documentId.ToString(),
            payload,
            DateTimeOffset.UtcNow);

        await listener.HandleDocumentUploadedAsync(message, CancellationToken.None);

        Assert.Equal(documentId, assignmentService.LastDocumentId);
        Assert.NotNull(ruleEngine.LastContext);
        Assert.True(ruleEngine.LastContext!.Fields.ContainsKey("groupIds"));
        Assert.False(ruleEngine.LastContext.Fields.ContainsKey("department"));
        Assert.Equal(TaggingRuleTrigger.DocumentUploaded, ruleEngine.LastTrigger);
    }

    [Fact]
    public async Task HandleOcrCompletedAsync_UsesOcrTrigger()
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
            EventTopics.Pipelines.Ocr.Completed,
            documentId.ToString(),
            payload,
            DateTimeOffset.UtcNow);

        await listener.HandleOcrCompletedAsync(message, CancellationToken.None);

        Assert.Equal(TaggingRuleTrigger.OcrCompleted, ruleEngine.LastTrigger);
        Assert.Equal(1, assignmentService.InvocationCount);
    }

    private static ServiceProvider BuildServiceProvider(
        RecordingRuleEngine ruleEngine,
        RecordingAssignmentService assignmentService)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<TaggingEventProcessor>();
        services.AddSingleton(ruleEngine);
        services.AddSingleton(assignmentService);
        services.AddScoped<ITaggingRuleEngine>(_ => ruleEngine);
        services.AddScoped<IDocumentTagAssignmentService>(_ => assignmentService);
        return services.BuildServiceProvider();
    }
}
