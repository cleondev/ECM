using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
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

    public static string CreateLoginUrl(HttpContext context, string redirectPath)
    {
        ArgumentNullException.ThrowIfNull(context);

        var loginPath = CreateLoginPath(redirectPath);
        var queryIndex = loginPath.IndexOf('?');

        var path = queryIndex >= 0
            ? new PathString(loginPath[..queryIndex])
            : new PathString(loginPath);

        var query = queryIndex >= 0
            ? new QueryString(loginPath[queryIndex..])
            : QueryString.Empty;

        return UriHelper.BuildAbsolute(
            context.Request.Scheme,
            context.Request.Host,
            context.Request.PathBase,
            path,
            query);
    }
}
