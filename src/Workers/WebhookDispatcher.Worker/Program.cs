using ECM.Webhook.Application;
using ECM.Webhook.Application.Dispatching;
using ECM.Webhook.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using ServiceDefaults;
using Workers.Shared.Messaging;
using Workers.Shared.Messaging.Kafka;

namespace WebhookDispatcher;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Starting WebhookDispatcher worker");

            var builder = Host.CreateApplicationBuilder(args);

            builder.AddServiceDefaults();

            ConfigureKafka(builder);
            ConfigureWebhookDispatcher(builder);

            builder.Services.AddHostedService<WebhookRequestListener>();

            var host = builder.Build();

            await host.RunAsync().ConfigureAwait(false);

            Log.Information("WebhookDispatcher worker stopped");
        }
        catch (Exception exception)
        {
            Log.Fatal(exception, "WebhookDispatcher worker terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void ConfigureKafka(HostApplicationBuilder builder)
    {
        builder.Services.Configure<KafkaConsumerOptions>(builder.Configuration.GetSection(KafkaConsumerOptions.SectionName));
        builder.Services.PostConfigure<KafkaConsumerOptions>(options =>
        {
            options.GroupId ??= "webhook-dispatcher";

            if (!string.IsNullOrWhiteSpace(options.BootstrapServers))
            {
                return;
            }

            var connectionString = builder.Configuration.GetConnectionString("kafka");
            var bootstrapServers = KafkaConnectionStringParser.ExtractBootstrapServers(connectionString);

            if (!string.IsNullOrWhiteSpace(bootstrapServers))
            {
                options.BootstrapServers = bootstrapServers;
            }
        });

        builder.Services.AddSingleton<IKafkaConsumer, KafkaConsumer>();
    }

    private static void ConfigureWebhookDispatcher(HostApplicationBuilder builder)
    {
        builder.Services.Configure<WebhookDispatcherOptions>(builder.Configuration.GetSection(WebhookDispatcherOptions.SectionName));
        builder.Services.PostConfigure<WebhookDispatcherOptions>(options =>
        {
            options.MaxRetryAttempts = Math.Max(1, options.MaxRetryAttempts);

            if (options.Endpoints is not Dictionary<string, string> endpoints ||
                endpoints.Comparer != StringComparer.OrdinalIgnoreCase)
            {
                options.Endpoints = new Dictionary<string, string>(options.Endpoints, StringComparer.OrdinalIgnoreCase);
            }

            if (options.InitialBackoff <= TimeSpan.Zero)
            {
                options.InitialBackoff = TimeSpan.FromSeconds(2);
            }

            if (options.Endpoints.Count == 0)
            {
                options.Endpoints["UploadCallback_PGB"] = "https://localhost:7081/api/upload-callback";
            }
        });

        builder.Services.AddWebhookApplication();
        builder.Services.AddWebhookInfrastructure();
        builder.Services.AddHttpClient<WebhookDispatchService>();
    }
}
