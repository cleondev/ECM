using ECM.Modules.Abstractions.Persistence;
using EFCore.NamingConventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace ECM.IAM.Infrastructure.Persistence;

public sealed class IamDbContextFactory : IDesignTimeDbContextFactory<IamDbContext>
{
    private const string ConnectionStringName = "IAM";

    public IamDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IamDbContext>();
        var configuration = DesignTimeDbContextFactoryHelper.BuildConfiguration<IamDbContextFactory>();
        var connectionString = DesignTimeDbContextFactoryHelper.ResolveConnectionString<IamDbContextFactory>(
            configuration,
            ConnectionStringName);

        optionsBuilder
            .UseNpgsql(
                connectionString,
                builder => builder
                    .MigrationsAssembly(typeof(IamDbContext).Assembly.FullName)
                    .MigrationsHistoryTable("__EFMigrationsHistory", "iam"))
            .UseSnakeCaseNamingConvention();

        return new IamDbContext(optionsBuilder.Options);
    }
}
