using ECM.Modules.Abstractions.Persistence;
using EFCore.NamingConventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace ECM.File.Infrastructure.Persistence;

public sealed class FileDbContextFactory : IDesignTimeDbContextFactory<FileDbContext>
{
    private const string ConnectionStringName = "File";

    public FileDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FileDbContext>();
        var configuration = DesignTimeDbContextFactoryHelper.BuildConfiguration<FileDbContextFactory>();
        var connectionString = DesignTimeDbContextFactoryHelper.ResolveConnectionString<FileDbContextFactory>(
            configuration,
            ConnectionStringName);

        optionsBuilder
            .UseNpgsql(
                connectionString,
                builder => builder
                    .MigrationsAssembly(typeof(FileDbContext).Assembly.FullName)
                    .MigrationsHistoryTable("__EFMigrationsHistory", "file"))
            .UseSnakeCaseNamingConvention();

        return new FileDbContext(optionsBuilder.Options);
    }
}
