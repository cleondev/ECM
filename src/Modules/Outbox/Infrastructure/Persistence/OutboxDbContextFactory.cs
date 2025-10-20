using ECM.Modules.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ECM.Outbox.Infrastructure.Persistence;

public sealed class OutboxDbContextFactory : IDesignTimeDbContextFactory<OutboxDbContext>
{
    public OutboxDbContext CreateDbContext(string[] args)
    {
        var configuration = DesignTimeDbContextFactoryHelper.BuildConfiguration<OutboxDbContextFactory>();
        var connectionString = DesignTimeDbContextFactoryHelper.ResolveConnectionString<OutboxDbContextFactory>(
            configuration,
            "ConnectionStrings:Operations");

        var builder = new DbContextOptionsBuilder<OutboxDbContext>();
        builder.UseNpgsql(connectionString, options => options.MigrationsHistoryTable("_ef_migrations", "ops"));
        builder.UseSnakeCaseNamingConvention();

        return new OutboxDbContext(builder.Options);
    }
}
