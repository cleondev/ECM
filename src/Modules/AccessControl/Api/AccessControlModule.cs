using ECM.Abstractions;
using ECM.AccessControl.Api.Relations;
using ECM.AccessControl.Api.Roles;
using ECM.AccessControl.Api.Users;
using ECM.AccessControl.Application;
using ECM.AccessControl.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace ECM.AccessControl.Api;

public sealed class AccessControlModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAccessControlApplication();
        services.AddAccessControlInfrastructure();
        services.ConfigureModuleSwagger(AccessControlSwagger.DocumentName, AccessControlSwagger.Info);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapUserEndpoints();
        endpoints.MapUserProfileEndpoints();
        endpoints.MapRoleEndpoints();
        endpoints.MapRelationEndpoints();
    }
}

internal static class AccessControlSwagger
{
    internal const string DocumentName = "access-control";

    internal static readonly OpenApiInfo Info = new()
    {
        Title = "Access Control API",
        Version = "v1",
        Description = "Endpoints that manage users, roles, relations and permissions."
    };
}
