using System;
using ECM.SearchIndexer.Application;
using ECM.SearchIndexer.Infrastructure;
using Microsoft.Extensions.Hosting;
using Serilog;
using ServiceDefaults;

namespace SearchIndexer;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Starting SearchIndexer worker");

            var builder = Host.CreateApplicationBuilder(args);

            builder.AddServiceDefaults();

            builder.Services.AddSearchIndexerApplication();
            builder.Services.AddSearchIndexerInfrastructure();

            var host = builder.Build();

            await host.RunAsync().ConfigureAwait(false);

            Log.Information("SearchIndexer worker stopped");
        }
        catch (Exception exception)
        {
            Log.Fatal(exception, "SearchIndexer worker terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
