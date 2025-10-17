using System;
using System.Data.Common;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace ECM.BuildingBlocks.Infrastructure.Persistence;

public static class DesignTimeDbContextFactoryHelper
{
    public static IConfiguration BuildConfiguration<TFactory>()
        where TFactory : class
    {
        var factoryName = typeof(TFactory).Name;
        var basePath = Directory.GetCurrentDirectory();
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        Console.WriteLine($"[{factoryName}] Current directory: {basePath}");
        Console.WriteLine($"[{factoryName}] Environment: {environment}");

        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true);

        if (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
        {
            configurationBuilder.AddUserSecrets(typeof(TFactory).Assembly, optional: true);
        }

        configurationBuilder.AddEnvironmentVariables();

        return configurationBuilder.Build();
    }

    public static string ResolveConnectionString<TFactory>(
        IConfiguration configuration,
        string connectionStringName)
        where TFactory : class
    {
        var factoryName = typeof(TFactory).Name;

        Console.WriteLine($"[{factoryName}] Resolving connection string for key '{connectionStringName}'.");

        var environmentVariableKey = $"ConnectionStrings__{connectionStringName}";
        var environmentValue = Environment.GetEnvironmentVariable(environmentVariableKey);

        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            Console.WriteLine(
                $"[{factoryName}] Using value from environment variable '{environmentVariableKey}': {MaskConnectionString(environmentValue)}");

            return environmentValue;
        }

        Console.WriteLine($"[{factoryName}] Environment variable '{environmentVariableKey}' is not set or empty.");

        var configurationValue = configuration.GetConnectionString(connectionStringName);
        if (!string.IsNullOrWhiteSpace(configurationValue))
        {
            Console.WriteLine(
                $"[{factoryName}] Using value from configuration 'ConnectionStrings:{connectionStringName}': {MaskConnectionString(configurationValue)}");

            return configurationValue;
        }

        var message =
            $"[{factoryName}] Connection string 'ConnectionStrings:{connectionStringName}' is not configured. Provide it via environment variable '{environmentVariableKey}' or configuration.";

        Console.WriteLine(message);

        throw new InvalidOperationException(message);
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
