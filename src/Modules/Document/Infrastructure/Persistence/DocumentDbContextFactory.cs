using EFCore.NamingConventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace ECM.Modules.Document.Infrastructure.Persistence;

public sealed class DocumentDbContextFactory : IDesignTimeDbContextFactory<DocumentDbContext>
{
    public DocumentDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DocumentDbContext>();
        var connectionString = "Host=localhost;Port=5432;Database=ecm;Username=postgres;Password=postgres";

        optionsBuilder
            .UseNpgsql(
                connectionString,
                builder => builder.MigrationsAssembly(typeof(DocumentDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention();

        return new DocumentDbContext(optionsBuilder.Options);
    }
}
