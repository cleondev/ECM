using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;

namespace AppGateway.Api.Auth;

internal static class AzureLoginRedirectHelper
{
    private const string SilentMode = "silent";
    private const string ModeQueryKey = "mode";
    private const string LoginModePropertyKey = "appgateway:login-mode";
    private static readonly PathString RootPath = new("/");

    internal enum AzureLoginMode
    {
        Interactive,
        Silent
    }

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

    public static string CreateLoginPath(string redirectPath, AzureLoginMode mode = AzureLoginMode.Interactive)
    {
        var loginPath = QueryHelpers.AddQueryString("/signin-azure", "redirectUri", redirectPath);

        if (mode == AzureLoginMode.Silent)
        {
            loginPath = QueryHelpers.AddQueryString(loginPath, ModeQueryKey, SilentMode);
        }

        return loginPath;
    }

    public static string CreateLoginUrl(
        HttpContext context,
        string redirectPath,
        AzureLoginMode mode = AzureLoginMode.Interactive)
    {
        ArgumentNullException.ThrowIfNull(context);

        var loginPath = CreateLoginPath(redirectPath, mode);
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

    public static AuthenticationProperties CreateAuthenticationProperties(
        string redirectPath,
        AzureLoginMode mode)
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectPath
        };

        if (mode == AzureLoginMode.Silent)
        {
            properties.Items[LoginModePropertyKey] = SilentMode;
        }

        return properties;
    }

    public static AzureLoginMode ResolveLoginMode(string? mode)
    {
        return string.Equals(mode, SilentMode, StringComparison.OrdinalIgnoreCase)
            ? AzureLoginMode.Silent
            : AzureLoginMode.Interactive;
    }

    public static AzureLoginMode ResolveLoginMode(AuthenticationProperties? properties)
    {
        if (properties is null)
        {
            return AzureLoginMode.Interactive;
        }

        if (properties.Items.TryGetValue(LoginModePropertyKey, out var value)
            && string.Equals(value, SilentMode, StringComparison.Ordinal))
        {
            return AzureLoginMode.Silent;
        }

        return AzureLoginMode.Interactive;
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
