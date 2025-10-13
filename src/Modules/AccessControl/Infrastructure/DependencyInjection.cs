using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.BuildingBlocks.Infrastructure.Configuration;
using ECM.BuildingBlocks.Infrastructure.Time;
using ECM.AccessControl.Application.Relations;
using ECM.AccessControl.Application.Roles;
using ECM.AccessControl.Application.Users;
using ECM.AccessControl.Infrastructure.Persistence;
using ECM.AccessControl.Infrastructure.Relations;
using ECM.AccessControl.Infrastructure.Roles;
using ECM.AccessControl.Infrastructure.Users;
using EFCore.NamingConventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace Microsoft.Extensions.DependencyInjection;

public static class AccessControlInfrastructureModuleExtensions
{
    public static IServiceCollection AddAccessControlInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISystemClock, SystemClock>();

        services.AddDbContext<AccessControlDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetRequiredConnectionStringForModule("AccessControl");

            options
                .UseNpgsql(
                    connectionString,
                    builder => builder.MigrationsAssembly(typeof(AccessControlDbContext).Assembly.FullName))
                .UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IAccessRelationRepository, AccessRelationRepository>();

        return services;
    }
}
