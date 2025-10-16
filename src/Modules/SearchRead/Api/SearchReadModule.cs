using ECM.Abstractions;
using ECM.SearchRead.Api.Search;
using ECM.SearchRead.Application;
using ECM.SearchRead.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace ECM.SearchRead.Api;

public sealed class SearchReadModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSearchReadApplication();
        services.AddSearchReadInfrastructure();
        services.ConfigureModuleSwagger(SearchReadSwagger.DocumentName, SearchReadSwagger.Info);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapSearchEndpoints();
    }
}

internal static class SearchReadSwagger
{
    internal const string DocumentName = "search";

    internal static readonly OpenApiInfo Info = new()
    {
        Title = "Search API",
        Version = "v1",
        Description = "Full-text search and retrieval endpoints for ECM content."
    };
}
