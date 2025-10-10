using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.BuildingBlocks.Infrastructure.Time;
using ECM.Modules.AccessControl.Domain.Relations;
using ECM.Modules.AccessControl.Domain.Roles;
using ECM.Modules.AccessControl.Domain.Users;
using ECM.Modules.AccessControl.Infrastructure.Persistence;
using ECM.Modules.AccessControl.Infrastructure.Relations;
using ECM.Modules.AccessControl.Infrastructure.Roles;
using ECM.Modules.AccessControl.Infrastructure.Users;
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
            var connectionString = configuration.GetConnectionString("AccessControl")
                ?? configuration.GetConnectionString("postgres")
                ?? throw new InvalidOperationException("Access control database connection string is not configured.");

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
