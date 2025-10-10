namespace Microsoft.Extensions.DependencyInjection;

using ECM.Modules.AccessControl.Application.Relations;
using ECM.Modules.AccessControl.Application.Roles;
using ECM.Modules.AccessControl.Application.Users;

public static class AccessControlApplicationModuleExtensions
{
    public static IServiceCollection AddAccessControlApplication(this IServiceCollection services)
    {
        services.AddScoped<UserApplicationService>();
        services.AddScoped<RoleApplicationService>();
        services.AddScoped<AccessRelationApplicationService>();

        return services;
    }
}
