using ECM.Modules.Abstractions;
using ECM.Modules.File.Api.Files;
using ECM.Modules.File.Application;
using ECM.Modules.File.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.Modules.File.Api;

public sealed class FileModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddFileApplication();
        services.AddFileInfrastructure();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapFileEndpoints();
    }
}
