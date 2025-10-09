using ECM.Modules.Abstractions;
using ECM.Modules.SearchRead.Api.Search;
using ECM.Modules.SearchRead.Application;
using ECM.Modules.SearchRead.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.Modules.SearchRead.Api;

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
