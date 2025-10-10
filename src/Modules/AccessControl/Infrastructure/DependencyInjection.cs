using EFCore.NamingConventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace Microsoft.Extensions.DependencyInjection;

using ECM.Modules.AccessControl.Infrastructure.Persistence;

public static class AccessControlInfrastructureModuleExtensions
{
    public static IServiceCollection AddAccessControlInfrastructure(this IServiceCollection services)
    {
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

        return services;
    }
}
