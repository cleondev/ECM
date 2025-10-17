using ECM.Modules.Abstractions.Persistence;
using EFCore.NamingConventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace ECM.Document.Infrastructure.Persistence;

public sealed class DocumentDbContextFactory : IDesignTimeDbContextFactory<DocumentDbContext>
{
    private const string ConnectionStringName = "Document";

    public DocumentDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DocumentDbContext>();
        var configuration = DesignTimeDbContextFactoryHelper.BuildConfiguration<DocumentDbContextFactory>();
        var connectionString = DesignTimeDbContextFactoryHelper.ResolveConnectionString<DocumentDbContextFactory>(
            configuration,
            ConnectionStringName);

        optionsBuilder
            .UseNpgsql(
                connectionString,
                builder => builder
                    .MigrationsAssembly(typeof(DocumentDbContext).Assembly.FullName)
                    .MigrationsHistoryTable("__EFMigrationsHistory", "doc"))
            .UseSnakeCaseNamingConvention();

        return new DocumentDbContext(optionsBuilder.Options);
    }
}
