using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<NotificationWorker> _logger;

    public NotificationWorker(ILogger<NotificationWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // TODO: Deliver email/webhook notifications.
                _logger.LogDebug("Waiting {Delay} before polling for pending notifications.", TimeSpan.FromMinutes(1));
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown requested. The loop will exit naturally.
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unexpected error while processing notifications. Retrying shortly.");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("Notification worker is stopping.");
    }
}
