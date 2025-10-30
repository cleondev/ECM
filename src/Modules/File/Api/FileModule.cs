using ECM.Abstractions;
using ECM.File.Api.Files;
using ECM.File.Application;
using ECM.File.Api.Shares;
using ECM.File.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace ECM.File.Api;

public sealed class FileModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddFileApplication();
        services.AddFileInfrastructure();
        services.ConfigureModuleSwagger(FileSwagger.DocumentName, FileSwagger.Info);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapFileEndpoints();
        endpoints.MapShareEndpoints();
        endpoints.MapPublicShareEndpoints();
    }
}

internal static class FileSwagger
{
    internal const string DocumentName = "files";

    internal static readonly OpenApiInfo Info = new()
    {
        Title = "Files API",
        Version = "v1",
        Description = "File storage and retrieval endpoints for ECM-managed assets."
    };
}
