using ECM.Abstractions;
using ECM.SearchRead.Api.Search;
using ECM.SearchRead.Application;
using ECM.SearchRead.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.SearchRead.Api;

public sealed class SearchReadModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSearchReadApplication();
        services.AddSearchReadInfrastructure();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapSearchEndpoints();
    }
}
