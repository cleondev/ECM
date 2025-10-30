using System;
using System.Security.Claims;

namespace ECM.Document.Api.Tags;

internal static class TagNamespaceSlugResolver
{
    private static readonly string[] EmailClaimTypes =
    [
        ClaimTypes.Email,
        "preferred_username",
        ClaimTypes.Upn
    ];

    public static string Resolve(string? namespaceSlug, ClaimsPrincipal? principal)
    {
        if (string.IsNullOrWhiteSpace(namespaceSlug))
        {
            return namespaceSlug ?? string.Empty;
        }

        var normalized = namespaceSlug.Trim().ToLowerInvariant();

        if (!string.Equals(normalized, "user", StringComparison.Ordinal))
        {
            return normalized;
        }

        var email = GetNormalizedEmail(principal);
        if (string.IsNullOrWhiteSpace(email))
        {
            return normalized;
        }

        return $"{normalized}/{email}";
    }

    private static string? GetNormalizedEmail(ClaimsPrincipal? principal)
    {
        if (principal is null)
        {
            return null;
        }

        foreach (var claimType in EmailClaimTypes)
        {
            var value = principal.FindFirst(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim().ToLowerInvariant();
            }
        }

        return null;
    }
}
