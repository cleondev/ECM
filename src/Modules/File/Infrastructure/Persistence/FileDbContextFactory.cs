using EFCore.NamingConventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace ECM.File.Infrastructure.Persistence;

public sealed class FileDbContextFactory : IDesignTimeDbContextFactory<FileDbContext>
{
    public FileDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FileDbContext>();
        var connectionString = "Host=localhost;Port=5432;Database=ecm;Username=postgres;Password=postgres";

        optionsBuilder
            .UseNpgsql(
                connectionString,
                builder => builder.MigrationsAssembly(typeof(FileDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention();

        return new FileDbContext(optionsBuilder.Options);
    }
}
