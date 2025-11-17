using System;
using System.Threading.Tasks;
using ECM.Document.Application;
using ECM.Document.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using ServiceDefaults;
using Workers.Shared.Messaging;

namespace Tagger;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Starting Tagger worker");

            var builder = Host.CreateApplicationBuilder(args);
            builder.AddServiceDefaults();

            builder.Services.AddDocumentApplication();
            builder.Services.AddDocumentInfrastructure();

            builder.Services.AddSingleton<IValidateOptions<TaggingRulesOptions>, TaggingRulesOptionsValidator>();
            builder.Services
                .AddOptions<TaggingRulesOptions>()
                .Bind(builder.Configuration.GetSection(TaggingRulesOptions.SectionName))
                .ValidateOnStart();

            builder.Services.AddSingleton<ITaggingRuleEngine, TaggingRuleEngine>();
            builder.Services.AddScoped<IDocumentTagAssignmentService, DocumentTagAssignmentService>();
            builder.Services.AddScoped<TaggingEventProcessor>();

            builder.Services.Configure<KafkaConsumerOptions>(builder.Configuration.GetSection(KafkaConsumerOptions.SectionName));
            builder.Services.PostConfigure<KafkaConsumerOptions>(options =>
            {
                options.GroupId ??= "tagger-worker";

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
            builder.Services.AddHostedService<TaggingIntegrationEventListener>();

            var host = builder.Build();
            await host.RunAsync().ConfigureAwait(false);

            Log.Information("Tagger worker stopped");
        }
        catch (Exception exception)
        {
            Log.Fatal(exception, "Tagger worker terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
