using System.Threading;
using System.Threading.Tasks;
using ECM.SearchIndexer.Application.Indexing.Abstractions;
using ECM.SearchIndexer.Domain.Indexing;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace ECM.SearchIndexer.Infrastructure.Indexing;

internal sealed class HangfireIndexingJobScheduler(
    IBackgroundJobClient backgroundJobClient,
    ILogger<HangfireIndexingJobScheduler> logger) : IIndexingJobScheduler
{
    private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient;
    private readonly ILogger<HangfireIndexingJobScheduler> _logger = logger;

    public Task<string> EnqueueAsync(SearchIndexRecord record, CancellationToken cancellationToken = default)
    {
        var jobId = _backgroundJobClient.Enqueue<SearchIndexingJob>(job => job.ExecuteAsync(record, CancellationToken.None));
        _logger.LogInformation("Enqueued search indexing job {JobId} for document {DocumentId}", jobId, record.DocumentId);
        return Task.FromResult(jobId);
    }
}
