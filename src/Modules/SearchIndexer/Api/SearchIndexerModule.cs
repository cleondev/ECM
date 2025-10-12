using ECM.Abstractions;
using ECM.SearchIndexer.Api.Indexing;
using ECM.SearchIndexer.Application;
using ECM.SearchIndexer.Infrastructure;
using Hangfire;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.SearchIndexer.Api;

public sealed class SearchIndexerModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSearchIndexerApplication();
        services.AddSearchIndexerInfrastructure();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapSearchIndexingEndpoints();
        endpoints.MapHangfireDashboard("/hangfire/search-indexer");
    }
}
