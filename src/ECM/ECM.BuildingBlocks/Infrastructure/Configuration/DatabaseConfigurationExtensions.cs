using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace ECM.BuildingBlocks.Infrastructure.Configuration;

public static class DatabaseConfigurationExtensions
{
    private const string DatabaseSectionName = "Database";
    private const string ConnectionsSectionName = "Connections";
    private const string SchemasSectionName = "Schemas";
    private const string DefaultConnectionName = "postgres";

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

        var schemaName = configuration[$"{DatabaseSectionName}:{SchemasSectionName}:{moduleName}"];

        foreach (var candidate in EnumerateCandidates(moduleName, schemaName))
        {
            var connectionString = ResolveConnectionString(configuration, candidate);
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                return connectionString;
            }
        }

        throw new InvalidOperationException($"Database connection string for module '{moduleName}' is not configured.");
    }

    private static IEnumerable<string> EnumerateCandidates(string moduleName, string? schemaName)
    {
        if (!string.IsNullOrWhiteSpace(moduleName))
        {
            yield return moduleName;
        }

        if (!string.IsNullOrWhiteSpace(schemaName))
        {
            yield return schemaName;
        }

        yield return DefaultConnectionName;
    }

    private static string? ResolveConnectionString(IConfiguration configuration, string key)
    {
        var connectionString = configuration.GetConnectionString(key);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        var databaseConnections = configuration.GetSection($"{DatabaseSectionName}:{ConnectionsSectionName}");
        if (!databaseConnections.Exists())
        {
            return null;
        }

        var connectionFromCustomSection = databaseConnections[key];
        return string.IsNullOrWhiteSpace(connectionFromCustomSection) ? null : connectionFromCustomSection;
    }
}
