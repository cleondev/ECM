using ECM.Abstractions;
using ECM.AccessControl.Api.Relations;
using ECM.AccessControl.Api.Roles;
using ECM.AccessControl.Api.Users;
using ECM.AccessControl.Application;
using ECM.AccessControl.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.AccessControl.Api;

public sealed class AccessControlModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAccessControlApplication();
        services.AddAccessControlInfrastructure();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapUserEndpoints();
        endpoints.MapRoleEndpoints();
        endpoints.MapRelationEndpoints();
    }
}
