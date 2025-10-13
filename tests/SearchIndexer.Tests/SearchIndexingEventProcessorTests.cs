using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.SearchIndexer.Application.Events;
using ECM.SearchIndexer.Application.Indexing;
using ECM.SearchIndexer.Application.Indexing.Abstractions;
using ECM.SearchIndexer.Domain.Indexing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace SearchIndexer.Tests;

public class SearchIndexingEventProcessorTests
{
    [Fact]
    public async Task HandleDocumentUploadedAsync_SchedulesBasicIndexing()
    {
        var scheduler = new RecordingScheduler();
        var handler = new EnqueueDocumentIndexingHandler(scheduler, NullLogger<EnqueueDocumentIndexingHandler>.Instance);
        var processor = new SearchIndexingEventProcessor(handler);

        var @event = new DocumentUploadedEvent(
            Guid.NewGuid(),
            "Quarterly Report",
            "Summary",
            "Initial content",
            new Dictionary<string, string> { { "category", "finance" } },
            new[] { "q1", "internal" });

        var result = await processor.HandleDocumentUploadedAsync(@event).ConfigureAwait(false);

        Assert.NotNull(result);
        Assert.NotNull(scheduler.LastRecord);
        Assert.Equal(@event.DocumentId, scheduler.LastRecord!.DocumentId);
        Assert.Equal(SearchIndexingType.Basic, scheduler.LastRecord.IndexingType);
        Assert.Equal("Quarterly Report", scheduler.LastRecord.Title);
    }

    [Fact]
    public async Task HandleOcrCompletedAsync_SchedulesAdvancedIndexing()
    {
        var scheduler = new RecordingScheduler();
        var handler = new EnqueueDocumentIndexingHandler(scheduler, NullLogger<EnqueueDocumentIndexingHandler>.Instance);
        var processor = new SearchIndexingEventProcessor(handler);

        var @event = new OcrCompletedEvent(
            Guid.NewGuid(),
            "Scanned Contract",
            "Signed contract",
            "Full OCR extracted text",
            null,
            new[] { "legal" });

        await processor.HandleOcrCompletedAsync(@event).ConfigureAwait(false);

        Assert.NotNull(scheduler.LastRecord);
        Assert.Equal(SearchIndexingType.Advanced, scheduler.LastRecord!.IndexingType);
        Assert.Equal(@event.DocumentId, scheduler.LastRecord.DocumentId);
        Assert.Contains("Scanned Contract", scheduler.LastRecord.Content, StringComparison.Ordinal);
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
