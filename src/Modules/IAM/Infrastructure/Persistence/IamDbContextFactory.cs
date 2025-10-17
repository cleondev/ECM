using EFCore.NamingConventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.Extensions.Configuration;
using System.Data.Common;

namespace ECM.IAM.Infrastructure.Persistence;

public sealed class IamDbContextFactory : IDesignTimeDbContextFactory<IamDbContext>
{
    private const string ConnectionStringName = "IAM";
    private const string DefaultConnectionString = "Host=localhost;Port=5432;Database=ecm;Username=postgres;Password=postgres";

    public IamDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IamDbContext>();
        var configuration = BuildConfiguration();
        var connectionString = ResolveConnectionString(configuration);

        optionsBuilder
            .UseNpgsql(
                connectionString,
                builder => builder.MigrationsAssembly(typeof(IamDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention();

        return new IamDbContext(optionsBuilder.Options);
    }

    private static IConfiguration BuildConfiguration()
    {
        var basePath = Directory.GetCurrentDirectory();
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        Console.WriteLine($"[IamDbContextFactory] Current directory: {basePath}");
        Console.WriteLine($"[IamDbContextFactory] Environment: {environment}");

        return new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    private static string ResolveConnectionString(IConfiguration configuration)
    {
        Console.WriteLine($"[IamDbContextFactory] Resolving connection string for key '{ConnectionStringName}'.");

        var environmentVariableKey = $"ConnectionStrings__{ConnectionStringName}";
        var environmentValue = Environment.GetEnvironmentVariable(environmentVariableKey);

        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            Console.WriteLine(
                $"[IamDbContextFactory] Using value from environment variable '{environmentVariableKey}': {MaskConnectionString(environmentValue)}");

            return environmentValue;
        }

        Console.WriteLine($"[IamDbContextFactory] Environment variable '{environmentVariableKey}' is not set or empty.");

        var configurationValue = configuration.GetConnectionString(ConnectionStringName);
        if (!string.IsNullOrWhiteSpace(configurationValue))
        {
            Console.WriteLine(
                $"[IamDbContextFactory] Using value from configuration 'ConnectionStrings:{ConnectionStringName}': {MaskConnectionString(configurationValue)}");

            return configurationValue;
        }

        Console.WriteLine(
            $"[IamDbContextFactory] Connection string 'ConnectionStrings:{ConnectionStringName}' not found in configuration. Falling back to default value: {MaskConnectionString(DefaultConnectionString)}");

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
