using Microsoft.Extensions.Hosting;
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
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // TODO: Consume events and build search indexes.
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
