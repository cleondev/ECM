using ECM.Modules.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ECM.Operations.Infrastructure.Persistence;

public sealed class OperationsDbContextFactory : IDesignTimeDbContextFactory<OperationsDbContext>
{
    public OperationsDbContext CreateDbContext(string[] args)
    {
        var configuration = DesignTimeDbContextFactoryHelper.BuildConfiguration<OperationsDbContextFactory>();
        var connectionString = DesignTimeDbContextFactoryHelper.ResolveConnectionString<OperationsDbContextFactory>(
            configuration,
            "ConnectionStrings:Operations");

        var builder = new DbContextOptionsBuilder<OperationsDbContext>();
        builder.UseNpgsql(connectionString, options => options.MigrationsHistoryTable("_ef_migrations", "ops"));
        builder.UseSnakeCaseNamingConvention();

        return new OperationsDbContext(builder.Options);
    }
}
