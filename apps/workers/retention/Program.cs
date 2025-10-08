using Microsoft.Extensions.Hosting;
using ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHostedService<RetentionWorker>();

await builder.Build().RunAsync();

class RetentionWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // TODO: Evaluate retention policies and purge expired content.
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
