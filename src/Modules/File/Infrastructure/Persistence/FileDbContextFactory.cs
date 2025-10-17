using EFCore.NamingConventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.Extensions.Configuration;
using System.Data.Common;

namespace ECM.File.Infrastructure.Persistence;

public sealed class FileDbContextFactory : IDesignTimeDbContextFactory<FileDbContext>
{
    private const string ConnectionStringName = "File";
    private const string DefaultConnectionString = "Host=localhost;Port=5432;Database=ecm;Username=postgres;Password=postgres";

    public FileDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FileDbContext>();
        var configuration = BuildConfiguration();
        var connectionString = ResolveConnectionString(configuration);

        optionsBuilder
            .UseNpgsql(
                connectionString,
                builder => builder.MigrationsAssembly(typeof(FileDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention();

        return new FileDbContext(optionsBuilder.Options);
    }

    private static IConfiguration BuildConfiguration()
    {
        var basePath = Directory.GetCurrentDirectory();
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        Console.WriteLine($"[FileDbContextFactory] Current directory: {basePath}");
        Console.WriteLine($"[FileDbContextFactory] Environment: {environment}");

        return new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    private static string ResolveConnectionString(IConfiguration configuration)
    {
        Console.WriteLine($"[FileDbContextFactory] Resolving connection string for key '{ConnectionStringName}'.");

        var environmentVariableKey = $"ConnectionStrings__{ConnectionStringName}";
        var environmentValue = Environment.GetEnvironmentVariable(environmentVariableKey);

        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            Console.WriteLine(
                $"[FileDbContextFactory] Using value from environment variable '{environmentVariableKey}': {MaskConnectionString(environmentValue)}");

            return environmentValue;
        }

        Console.WriteLine($"[FileDbContextFactory] Environment variable '{environmentVariableKey}' is not set or empty.");

        var configurationValue = configuration.GetConnectionString(ConnectionStringName);
        if (!string.IsNullOrWhiteSpace(configurationValue))
        {
            Console.WriteLine(
                $"[FileDbContextFactory] Using value from configuration 'ConnectionStrings:{ConnectionStringName}': {MaskConnectionString(configurationValue)}");

            return configurationValue;
        }

        Console.WriteLine(
            $"[FileDbContextFactory] Connection string 'ConnectionStrings:{ConnectionStringName}' not found in configuration. Falling back to default value: {MaskConnectionString(DefaultConnectionString)}");

        return DefaultConnectionString;
    }

    private static string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return "<empty>";
        }

        try
        {
            var builder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString,
            };

            if (builder.ContainsKey("Password"))
            {
                builder["Password"] = "********";
            }

            return builder.ConnectionString;
        }
        catch
        {
            return connectionString;
        }
    }
}
