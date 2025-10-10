namespace Microsoft.Extensions.DependencyInjection;

using ECM.AccessControl.Application.Relations;
using ECM.AccessControl.Application.Roles;
using ECM.AccessControl.Application.Users;

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
