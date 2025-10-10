using EFCore.NamingConventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace ECM.Modules.AccessControl.Infrastructure.Persistence;

public sealed class AccessControlDbContextFactory : IDesignTimeDbContextFactory<AccessControlDbContext>
{
    public AccessControlDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AccessControlDbContext>();
        var connectionString = "Host=localhost;Port=5432;Database=ecm;Username=postgres;Password=postgres";

        optionsBuilder
            .UseNpgsql(
                connectionString,
                builder => builder.MigrationsAssembly(typeof(AccessControlDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention();

        return new AccessControlDbContext(optionsBuilder.Options);
    }
}
