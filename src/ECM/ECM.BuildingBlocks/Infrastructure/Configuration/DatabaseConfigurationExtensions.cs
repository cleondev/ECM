using System;
using Microsoft.Extensions.Configuration;

namespace ECM.BuildingBlocks.Infrastructure.Configuration;

public static class DatabaseConfigurationExtensions
{
    public static string GetRequiredConnectionStringForModule(this IConfiguration configuration, string moduleName)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        if (string.IsNullOrWhiteSpace(moduleName))
        {
            throw new ArgumentException("Module name must be provided.", nameof(moduleName));
        }

        var connectionString = configuration.GetConnectionString(moduleName);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        throw new InvalidOperationException(
            $"Database connection string for module '{moduleName}' is not configured. " +
            $"Set 'ConnectionStrings:{moduleName}' via configuration or environment variables.");
    }
}
