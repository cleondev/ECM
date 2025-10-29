using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ECM.SearchIndexer.Application.Events;
using ECM.SearchIndexer.Application.Indexing;
using ECM.SearchIndexer.Application.Indexing.Abstractions;
using ECM.SearchIndexer.Domain.Indexing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SearchIndexer;
using Shared.Contracts.Messaging;
using Workers.Shared.Messaging;
using Xunit;

namespace SearchIndexer.Tests;

public class SearchIndexingIntegrationEventListenerTests
{
    private static readonly JsonSerializerOptions CachedWebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task HandleDocumentUploadedAsync_DeserializesAndDispatchesEvent()
    {
        var scheduler = new RecordingScheduler();
        using var provider = BuildServiceProvider(scheduler);

        var listener = new SearchIndexingIntegrationEventListener(
            new NoOpKafkaConsumer(NullLogger<NoOpKafkaConsumer>.Instance),
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<SearchIndexingIntegrationEventListener>.Instance);

        var documentId = Guid.NewGuid();
        string[] tags = ["hr", "employee"];
        var payload = JsonSerializer.Serialize(new
        {
            eventId = Guid.NewGuid(),
            occurredAtUtc = DateTimeOffset.UtcNow,
            data = new
            {
                documentId,
                title = "Onboarding Checklist",
                summary = "Steps for new hires",
                content = "Detailed onboarding plan",
                metadata = new
                {
                    groupIds = new[] { "grp-hr" }
                },
                tags
            }
        }, CachedWebOptions);

        var message = new KafkaMessage(
            EventTopics.Document.Uploaded,
            Key: documentId.ToString(),
            Value: payload,
            Timestamp: DateTimeOffset.UtcNow);

        await listener.HandleDocumentUploadedAsync(message, CancellationToken.None);

        Assert.NotNull(scheduler.LastRecord);
        Assert.Equal(documentId, scheduler.LastRecord!.DocumentId);
        Assert.Equal(SearchIndexingType.Basic, scheduler.LastRecord.IndexingType);
        Assert.True(scheduler.LastRecord.Metadata.TryGetValue("groupIds", out var groups));
        Assert.Equal("grp-hr", groups);
    }

    [Fact]
    public async Task HandleOcrCompletedAsync_DeserializesAndDispatchesEvent()
    {
        var scheduler = new RecordingScheduler();
        using var provider = BuildServiceProvider(scheduler);

        var listener = new SearchIndexingIntegrationEventListener(
            new NoOpKafkaConsumer(NullLogger<NoOpKafkaConsumer>.Instance),
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<SearchIndexingIntegrationEventListener>.Instance);

        var documentId = Guid.NewGuid();
        string[] tags = ["legal"];
        var payload = JsonSerializer.Serialize(new
        {
            eventId = Guid.NewGuid(),
            occurredAtUtc = DateTimeOffset.UtcNow,
            data = new
            {
                documentId,
                title = "Signed Contract",
                summary = "Customer contract",
                content = "OCR extracted body",
                metadata = new
                {
                    groupIds = new[] { "grp-legal", "grp-shared" },
                    source = "ocr"
                },
                tags
            }
        }, CachedWebOptions);

        var message = new KafkaMessage(
            EventTopics.Ocr.Completed,
            Key: documentId.ToString(),
            Value: payload,
            Timestamp: DateTimeOffset.UtcNow);

        await listener.HandleOcrCompletedAsync(message, CancellationToken.None);

        Assert.NotNull(scheduler.LastRecord);
        Assert.Equal(documentId, scheduler.LastRecord!.DocumentId);
        Assert.Equal(SearchIndexingType.Advanced, scheduler.LastRecord.IndexingType);
        Assert.True(scheduler.LastRecord.Metadata.TryGetValue("groupIds", out var ocrGroups));
        Assert.Equal("grp-legal,grp-shared", ocrGroups);
    }

    private static ServiceProvider BuildServiceProvider(IIndexingJobScheduler scheduler)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<SearchIndexingEventProcessor>();
        services.AddScoped<EnqueueDocumentIndexingHandler>();
        services.AddSingleton<IIndexingJobScheduler>(scheduler);
        return services.BuildServiceProvider();
    }

    private sealed class RecordingScheduler : IIndexingJobScheduler
    {
        public SearchIndexRecord? LastRecord { get; private set; }

        public Task<string> EnqueueAsync(SearchIndexRecord record, CancellationToken cancellationToken = default)
        {
            LastRecord = record;
            return Task.FromResult("job-id");
        }
    }
}
