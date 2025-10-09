using Microsoft.Extensions.Hosting;
using ServiceDefaults;

namespace Notify;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.AddServiceDefaults();

        builder.Services.AddHostedService<NotificationWorker>();

        await builder.Build().RunAsync();
    }
}

internal class NotificationWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // TODO: Deliver email/webhook notifications.
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
