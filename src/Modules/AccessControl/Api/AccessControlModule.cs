using ECM.Abstractions;
using ECM.Modules.AccessControl.Api.Relations;
using ECM.Modules.AccessControl.Api.Roles;
using ECM.Modules.AccessControl.Api.Users;
using ECM.Modules.AccessControl.Application;
using ECM.Modules.AccessControl.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.Modules.AccessControl.Api;

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
