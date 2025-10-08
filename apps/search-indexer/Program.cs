using Microsoft.Extensions.Hosting;
using ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddDefaultConfiguration()
       .AddServiceDefaults();

builder.Services.AddHostedService<SearchIndexerWorker>();

await builder.Build().RunAsync();

class SearchIndexerWorker : BackgroundService
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
