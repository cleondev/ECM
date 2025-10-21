using System;
using Microsoft.AspNetCore.WebUtilities;

namespace AppGateway.Api.Auth;

internal static class AzureLoginRedirectHelper
{
    public static string ResolveRedirectPath(string? candidate, string defaultPath, bool allowRoot = false)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return defaultPath;
        }

        if (!candidate.StartsWith("/", StringComparison.Ordinal))
        {
            return defaultPath;
        }

        if (candidate.StartsWith("//", StringComparison.Ordinal))
        {
            return defaultPath;
        }

        if (!allowRoot && string.Equals(candidate, "/", StringComparison.Ordinal))
        {
            return defaultPath;
        }

        return candidate;
    }

    public static string CreateLoginPath(string redirectPath)
        => QueryHelpers.AddQueryString("/signin-azure", "redirectUri", redirectPath);
}
