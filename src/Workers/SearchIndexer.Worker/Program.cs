using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServiceDefaults;

namespace SearchIndexer;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.AddServiceDefaults();

        builder.Services.AddHostedService<SearchIndexerWorker>();

        await builder.Build().RunAsync();
    }
}

internal class SearchIndexerWorker : BackgroundService
{
    private readonly ILogger<SearchIndexerWorker> _logger;

    public SearchIndexerWorker(ILogger<SearchIndexerWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Search indexer worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // TODO: Consume events and build search indexes.
                _logger.LogDebug("Waiting {Delay} before refreshing search indexes.", TimeSpan.FromSeconds(30));
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown requested. The loop will exit naturally.
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unexpected error while updating search indexes. Retrying shortly.");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("Search indexer worker is stopping.");
    }
}
