namespace ServiceDefaults.Authentication;

using System;

public static class AuthorityUtilities
{
    public static string? EnsureV2Authority(string? authority)
    {
        if (string.IsNullOrWhiteSpace(authority))
        {
            return authority;
        }

        var trimmed = authority.TrimEnd('/');

        return trimmed.EndsWith("/v2.0", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : string.Concat(trimmed, "/v2.0");
    }
}
