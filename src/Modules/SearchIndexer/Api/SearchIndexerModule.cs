using ECM.Abstractions;
using ECM.SearchIndexer.Api.Indexing;
using ECM.SearchIndexer.Application;
using ECM.SearchIndexer.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace ECM.SearchIndexer.Api;

public sealed class SearchIndexerModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSearchIndexerApplication();
        services.AddSearchIndexerInfrastructure();
        services.ConfigureModuleSwagger(SearchIndexerSwagger.DocumentName, SearchIndexerSwagger.Info);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapSearchIndexerEndpoints();
    }
}

internal static class SearchIndexerSwagger
{
    internal const string DocumentName = "search-indexer";

    internal static readonly OpenApiInfo Info = new()
    {
        Title = "Search Indexer API",
        Version = "v1",
        Description = "Manage search indexing workflows for ECM documents."
    };
}
