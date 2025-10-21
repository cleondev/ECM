using System;
using System.Collections.Generic;

namespace AppGateway.Infrastructure.Ecm;

internal static class ScopeUtilities
{
    private static readonly char[] Separators =
    [
        ' ',
        '\t',
        '\r',
        '\n',
        ',',
        ';'
    ];

    public static string[] ParseScopes(string? scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            return Array.Empty<string>();
        }

        var uniqueScopes = new List<string>();
        var seenScopes = new HashSet<string>(StringComparer.Ordinal);

        var segments = scope.Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var segment in segments)
        {
            if (string.IsNullOrWhiteSpace(segment))
            {
                continue;
            }

            if (seenScopes.Add(segment))
            {
                uniqueScopes.Add(segment);
            }
        }

        return uniqueScopes.Count == 0
            ? Array.Empty<string>()
            : [.. uniqueScopes];
    }
}
