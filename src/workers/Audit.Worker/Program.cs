using Microsoft.Extensions.Hosting;
using ServiceDefaults;

namespace Audit;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.AddServiceDefaults();

        builder.Services.AddHostedService<AuditWorker>();

        await builder.Build().RunAsync();
    }
}

internal class AuditWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // TODO: Append audit events to durable storage.
            await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken);
        }
    }
}
