using ECM.Ocr.Application;
using ECM.Ocr.Infrastructure;

using Serilog;

using ServiceDefaults;

using Workers.Shared.Messaging;

namespace Ocr;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Starting OCR worker");

            var builder = Host.CreateApplicationBuilder(args);

            builder.AddServiceDefaults();

            builder.Services.AddOcrApplication();
            builder.Services.AddOcrInfrastructure();
            builder.Services.Configure<KafkaConsumerOptions>(builder.Configuration.GetSection(KafkaConsumerOptions.SectionName));
            builder.Services.PostConfigure<KafkaConsumerOptions>(options =>
            {
                options.GroupId ??= "ocr-worker";

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
            builder.Services.AddHostedService<OcrProcessingIntegrationEventListener>();

            var host = builder.Build();

            await host.RunAsync().ConfigureAwait(false);

            Log.Information("OCR worker stopped");
        }
        catch (Exception exception)
        {
            Log.Fatal(exception, "OCR worker terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
