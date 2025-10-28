using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.BuildingBlocks.Infrastructure.Configuration;
using ECM.BuildingBlocks.Infrastructure.Time;
using ECM.IAM.Application.Relations;
using ECM.IAM.Application.Roles;
using ECM.IAM.Application.Users;
using ECM.IAM.Application.Users;
using ECM.IAM.Infrastructure.Persistence;
using ECM.IAM.Infrastructure.Relations;
using ECM.IAM.Infrastructure.Roles;
using ECM.IAM.Infrastructure.Users;
using ECM.IAM.Infrastructure.Security;

using EFCore.NamingConventions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace ECM.IAM.Infrastructure;

public static class IamInfrastructureModuleExtensions
{
    public static IServiceCollection AddIamInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISystemClock, SystemClock>();

        services.AddDbContext<IamDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetRequiredConnectionStringForModule("IAM");

            options
                .UseNpgsql(
                    connectionString,
                    builder => builder.MigrationsAssembly(typeof(IamDbContext).Assembly.FullName))
                .UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IAccessRelationRepository, AccessRelationRepository>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        return services;
    }
}
