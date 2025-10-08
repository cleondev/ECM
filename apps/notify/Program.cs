using Microsoft.Extensions.Hosting;
using ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddDefaultConfiguration()
       .AddServiceDefaults();

builder.Services.AddHostedService<NotificationWorker>();

await builder.Build().RunAsync();

class NotificationWorker : BackgroundService
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
