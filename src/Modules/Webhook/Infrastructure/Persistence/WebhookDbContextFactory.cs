using ECM.Modules.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ECM.Webhook.Infrastructure.Persistence;

public sealed class WebhookDbContextFactory : IDesignTimeDbContextFactory<WebhookDbContext>
{
    private const string ConnectionStringName = "Webhook";

    public WebhookDbContext CreateDbContext(string[] args)
    {
        var configuration = DesignTimeDbContextFactoryHelper.BuildConfiguration<WebhookDbContextFactory>();
        var connectionString = DesignTimeDbContextFactoryHelper.ResolveConnectionString<WebhookDbContextFactory>(
            configuration,
            ConnectionStringName);

        var builder = new DbContextOptionsBuilder<WebhookDbContext>();
        builder.UseNpgsql(connectionString, options => options.MigrationsHistoryTable("_ef_migrations", "webhook"));
        builder.UseSnakeCaseNamingConvention();

        return new WebhookDbContext(builder.Options);
    }
}
