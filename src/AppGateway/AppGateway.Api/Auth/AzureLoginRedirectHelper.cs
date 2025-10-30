using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;

namespace AppGateway.Api.Auth;

internal static class AzureLoginRedirectHelper
{
    private static readonly PathString RootPath = new("/");

    public static string ResolveRedirectPath(HttpContext context, string? candidate, string defaultPath, bool allowRoot = false)
    {
        ArgumentNullException.ThrowIfNull(context);

        var normalizedCandidate = TryNormalize(candidate, allowRoot);
        if (normalizedCandidate is not null)
        {
            return EnsureWithinPathBase(context.Request.PathBase, normalizedCandidate);
        }

        var normalizedDefault = TryNormalize(defaultPath, allowRoot);
        if (normalizedDefault is not null)
        {
            return EnsureWithinPathBase(context.Request.PathBase, normalizedDefault);
        }

        var fallback = FallbackDefault(defaultPath, allowRoot);
        return EnsureWithinPathBase(context.Request.PathBase, fallback);
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

    private static string? TryNormalize(string? value, bool allowRoot)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();

        if (!trimmed.StartsWith('/'))
        {
            return null;
        }

        if (trimmed.StartsWith("//", StringComparison.Ordinal))
        {
            return null;
        }

        var (pathSegment, suffix) = SplitPathAndSuffix(trimmed);
        var path = PathString.FromUriComponent(pathSegment);

        if (!path.HasValue)
        {
            return null;
        }

        if (!allowRoot && path == RootPath)
        {
            return null;
        }

        if (path == RootPath)
        {
            return suffix.Length > 0
                ? string.Concat("/", suffix)
                : "/";
        }

        var normalized = path.Value!;

        var normalizedPath = normalized.Length > 1
            ? normalized.TrimEnd('/')
            : normalized;

        return string.Concat(normalizedPath, suffix);
    }

    private static string FallbackDefault(string defaultPath, bool allowRoot)
    {
        if (allowRoot)
        {
            return "/";
        }

        if (string.IsNullOrWhiteSpace(defaultPath))
        {
            return "/app";
        }

        var trimmed = defaultPath.Trim();
        trimmed = trimmed.TrimEnd('/');

        return trimmed.StartsWith('/')
            ? trimmed
            : $"/{trimmed}";
    }

    private static string EnsureWithinPathBase(PathString pathBase, string target)
    {
        if (!pathBase.HasValue)
        {
            return target;
        }

        if (string.Equals(target, "/", StringComparison.Ordinal))
        {
            return pathBase.Value!;
        }

        var baseValue = pathBase.Value!;

        if (target.StartsWith(baseValue, StringComparison.Ordinal))
        {
            return target;
        }

        var (pathSegment, suffix) = SplitPathAndSuffix(target);
        var combined = pathBase.Add(PathString.FromUriComponent(pathSegment));
        var combinedPath = combined.Value ?? pathSegment;

        return string.Concat(combinedPath, suffix);
    }

    private static (string PathSegment, string Suffix) SplitPathAndSuffix(string value)
    {
        var queryIndex = value.IndexOf('?');
        var fragmentIndex = value.IndexOf('#');

        int suffixIndex;
        if (queryIndex < 0)
        {
            suffixIndex = fragmentIndex;
        }
        else if (fragmentIndex < 0)
        {
            suffixIndex = queryIndex;
        }
        else
        {
            suffixIndex = Math.Min(queryIndex, fragmentIndex);
        }

        if (suffixIndex < 0)
        {
            return (value, string.Empty);
        }

        return (value[..suffixIndex], value[suffixIndex..]);
    }
}
