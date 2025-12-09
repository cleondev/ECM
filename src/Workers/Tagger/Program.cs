using Ecm.Rules.Abstractions;
using Ecm.Rules.Engine;
using Ecm.Sdk.Authentication;
using Ecm.Sdk.Extensions;

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

            builder.Services.AddEcmSdk(builder.Configuration);

            builder.Services
                .AddOptions<EcmUserOptions>()
                .Bind(builder.Configuration.GetSection(EcmUserOptions.SectionName));

            builder.Services.AddSingleton<IEcmUserContext, ManualEcmUserContext>();

            builder.Services.AddSingleton<IValidateOptions<TaggerRulesOptions>, TaggerRulesOptionsValidator>();
            builder.Services
                .AddOptions<TaggerRulesOptions>()
                .Bind(builder.Configuration.GetSection(TaggerRulesOptions.SectionName))
                .ValidateOnStart();

            builder.Services.AddRuleEngine(options =>
            {
                options.ThrowIfRuleSetNotFound = false;
            });

            builder.Services.AddSingleton<IRuleProvider, TaggerRuleProvider>();

            builder.Services.AddSingleton<ITaggingRuleContextFactory, TaggingRuleContextFactory>();
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
           await Log.CloseAndFlushAsync();
        }
    }
}
