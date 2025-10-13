using ECM.SearchIndexer.Application;
using ECM.SearchIndexer.Infrastructure;
using Microsoft.Extensions.Hosting;
using ServiceDefaults;

namespace SearchIndexer;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.AddServiceDefaults();

        builder.Services.AddSearchIndexerApplication();
        builder.Services.AddSearchIndexerInfrastructure();

        await builder.Build().RunAsync();
    }
}
