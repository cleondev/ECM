using System;

namespace SearchIndexer.Messaging;

internal static class KafkaConnectionStringParser
{
    public static string? ExtractBootstrapServers(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        var trimmedConnectionString = connectionString.Trim();

        if (!trimmedConnectionString.Contains('=', StringComparison.Ordinal))
        {
            return trimmedConnectionString;
        }

        var segments = trimmedConnectionString.Split(
            ';',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var segment in segments)
        {
            var keyValue = segment.Split('=', 2, StringSplitOptions.TrimEntries);

            if (keyValue.Length != 2)
            {
                continue;
            }

            if (string.Equals(keyValue[0], "bootstrap.servers", StringComparison.OrdinalIgnoreCase))
            {
                return keyValue[1];
            }
        }

        return null;
    }
}
