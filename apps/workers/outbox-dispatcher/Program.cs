using Microsoft.Extensions.Hosting;
using ServiceDefaults;

namespace OutboxDispatcher;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.AddServiceDefaults();

        builder.Services.AddHostedService<OutboxDispatcherWorker>();

        await builder.Build().RunAsync();
    }
}

internal class OutboxDispatcherWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // TODO: Poll the outbox table and publish events to Redpanda.
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }
}
