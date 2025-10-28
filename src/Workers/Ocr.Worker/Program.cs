using ECM.Document.Infrastructure;
using ECM.File.Application;
using ECM.File.Infrastructure;
using ECM.Ocr.Application;
using ECM.Ocr.Infrastructure;
using ECM.Ocr.Infrastructure.DotOcr;

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
            builder.Services.AddDocumentInfrastructure();
            builder.Services.AddFileApplication();
            builder.Services.AddFileInfrastructure();
            builder.Services.PostConfigure<DotOcrOptions>(options =>
            {
                options.ChatCompletionsEndpoint = "v1/chat/completions";
                options.Model = "dotsocr-model";
                options.Temperature = 0;
                options.MaxTokens = 2048;
                options.Instruction = "Please output the layout information from the PDF image, including each layout element's bbox, its category, and the corresponding text content within the bbox.\n\n1. Bbox format: [x1, y1, x2, y2]\n\n2. Layout Categories: ['Caption', 'Footnote', 'Formula', 'List-item', 'Page-footer', 'Page-header', 'Picture', 'Section-header', 'Table', 'Text', 'Title'].\n\n3. Text Extraction & Formatting Rules:\n    - Picture: omit text.\n    - Formula: use LaTeX.\n    - Table: use HTML.\n    - All others: use Markdown.\n\n4. Output original text only, no translation.\n5. Return a single JSON object.";
            });
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
