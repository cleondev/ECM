using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Serilog;
using ServiceDefaults;

namespace OutboxDispatcher;

public static class Program

{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Starting OutboxDispatcher worker");

            var builder = Host.CreateApplicationBuilder(args);

            builder.AddServiceDefaults();

            ConfigureOutboxDispatcher(builder);

            builder.Services.AddHostedService<OutboxDispatcherWorker>();

            var host = builder.Build();

            await host.RunAsync().ConfigureAwait(false);

            Log.Information("OutboxDispatcher worker stopped");
        }
        catch (Exception exception)
        {
            Log.Fatal(exception, "OutboxDispatcher worker terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    /// <summary>
    ///     Registers the infrastructure required for the outbox dispatcher: configuration binding,
    ///     the shared PostgreSQL data source and the message processor service.
    /// </summary>
    private static void ConfigureOutboxDispatcher(HostApplicationBuilder builder)
    {
        builder.Services.Configure<OutboxDispatcherOptions>(builder.Configuration.GetSection("OutboxDispatcher"));
        builder.Services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<OutboxDispatcherOptions>>().Value;
            return options;
        });

        builder.Services.AddSingleton(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("Outbox")
                ?? configuration.GetConnectionString("Ops")
                ?? configuration.GetConnectionString("postgres")
                ?? throw new InvalidOperationException("Outbox database connection string is not configured.");

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            return dataSourceBuilder.Build();
        });

        builder.Services.AddSingleton<OutboxMessageProcessor>();
    }
}

/// <summary>
///     Background worker that keeps polling the outbox table and delegates the heavy lifting to
///     <see cref="OutboxMessageProcessor"/>. The worker only controls the cadence and error logging.
/// </summary>
internal class OutboxDispatcherWorker : BackgroundService
{
    private readonly OutboxMessageProcessor _processor;
    private readonly OutboxDispatcherOptions _options;
    private readonly ILogger<OutboxDispatcherWorker> _logger;

    public OutboxDispatcherWorker(
        OutboxMessageProcessor processor,
        OutboxDispatcherOptions options,
        ILogger<OutboxDispatcherWorker> logger)
    {
        _processor = processor;
        _options = options;
        _logger = logger;

        if (_options.PollInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Poll interval must be greater than zero.");
        }
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Outbox dispatcher started with poll interval {PollInterval} and batch size {BatchSize}.",
            _options.PollInterval,
            _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await _processor.ProcessBatchAsync(stoppingToken);

                if (processed == 0)
                {
                    await Task.Delay(_options.PollInterval, stoppingToken);
                }
                else
                {
                    _logger.LogInformation(
                        "Processed {ProcessedCount} outbox message(s) in the last batch.",
                        processed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown requested. The loop will exit naturally.
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unexpected error while dispatching outbox messages. Retrying shortly.");
                await Task.Delay(_options.PollInterval, stoppingToken);
            }
        }

        _logger.LogInformation("Outbox dispatcher is stopping.");
    }
}
