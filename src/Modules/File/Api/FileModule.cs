using ECM.Abstractions;
using ECM.File.Api.Files;
using ECM.File.Application;
using ECM.File.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.File.Api;

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
