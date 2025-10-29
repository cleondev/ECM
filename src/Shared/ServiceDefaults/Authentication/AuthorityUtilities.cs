namespace ServiceDefaults.Authentication;

using System;

public static class AuthorityUtilities
{
    public static string? EnsureV2Authority(string? authority, string? tenantId = null, string? instance = null)
    {
        var normalizedAuthority = NormalizeAuthority(authority, tenantId, instance);

        if (string.IsNullOrWhiteSpace(normalizedAuthority))
        {
            return normalizedAuthority;
        }

        var trimmed = normalizedAuthority.TrimEnd('/');

        return trimmed.EndsWith("/v2.0", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : string.Concat(trimmed, "/v2.0");
    }

    private static string? NormalizeAuthority(string? authority, string? tenantId, string? instance)
    {
        if (!string.IsNullOrWhiteSpace(authority))
        {
            return authority;
        }

        if (string.IsNullOrWhiteSpace(instance) || string.IsNullOrWhiteSpace(tenantId))
        {
            return authority;
        }

        var normalizedInstance = instance!.TrimEnd('/');
        return string.Concat(normalizedInstance, "/", tenantId);
    }
}
