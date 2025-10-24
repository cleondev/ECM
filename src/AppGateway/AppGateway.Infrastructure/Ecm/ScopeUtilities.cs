using System;
using System.Collections.Generic;

namespace AppGateway.Infrastructure.Ecm;

public static class ScopeUtilities
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
            return [];
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
            ? []
            : [.. uniqueScopes];
    }

    public static string? TryGetAppScope(string? scope)
    {
        var scopes = ParseScopes(scope);
        return TryGetAppScope(scopes);
    }

    public static string? TryGetAppScope(IReadOnlyList<string> scopes)
    {
        if (scopes.Count == 0)
        {
            return null;
        }

        foreach (var candidate in scopes)
        {
            if (candidate.EndsWith("/.default", StringComparison.Ordinal))
            {
                return candidate;
            }
        }

        foreach (var candidate in scopes)
        {
            var normalized = NormalizeToDefaultScope(candidate);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return normalized;
            }
        }

        return null;
    }

    private static string? NormalizeToDefaultScope(string scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            return null;
        }

        var schemeSeparatorIndex = scope.IndexOf(Uri.SchemeDelimiter, StringComparison.Ordinal);
        if (schemeSeparatorIndex < 0)
        {
            return null;
        }

        var firstSlashAfterAuthority = scope.IndexOf('/', schemeSeparatorIndex + Uri.SchemeDelimiter.Length);
        if (firstSlashAfterAuthority < 0)
        {
            return null;
        }

        var lastSlashIndex = scope.LastIndexOf('/');
        if (lastSlashIndex < firstSlashAfterAuthority)
        {
            return null;
        }

        var resource = scope[..lastSlashIndex];
        return string.IsNullOrWhiteSpace(resource)
            ? null
            : string.Concat(resource, "/.default");
    }
}
