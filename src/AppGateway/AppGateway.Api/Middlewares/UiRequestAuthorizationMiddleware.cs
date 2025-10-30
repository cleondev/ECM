using System;
using System.IO;
using System.Threading.Tasks;
using AppGateway.Api.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace AppGateway.Api.Middlewares;

internal sealed class UiRequestAuthorizationMiddleware
{
    private enum RouteExistence
    {
        Unknown,
        Missing,
        Present
    }

    private static readonly PathString ApiPathPrefix = new("/api");
    private static readonly PathString SignInPagePath = new("/signin");
    private static readonly PathString SignUpPagePath = new("/signup");
    private static readonly PathString LandingPagePath = new("/landing");
    private static readonly PathString SharePagePath = new("/s");
    private static readonly PathString AzureSignInPath = new("/signin-azure");
    private static readonly PathString AzureSignInUrlPath = new("/signin-azure/url");
    private static readonly PathString NextAssetPrefix = new("/_next");
    private static readonly PathString VercelAssetPrefix = new("/_vercel");
    private static readonly PathString SignInUiPath = new("/signin/");
    private static readonly PathString LandingRootPath = new("/");
    private static readonly PathString UiRequestPath = string.IsNullOrEmpty(Program.UiRequestPath)
        ? PathString.Empty
        : new PathString(Program.UiRequestPath);

    private readonly RequestDelegate _next;
    private readonly IFileProvider _fileProvider;
    private readonly IFileInfo _notFoundPage;
    private readonly ILogger<UiRequestAuthorizationMiddleware> _logger;

    public UiRequestAuthorizationMiddleware(
        RequestDelegate next,
        IWebHostEnvironment environment,
        ILogger<UiRequestAuthorizationMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        if (environment is null)
        {
            throw new ArgumentNullException(nameof(environment));
        }

        _fileProvider = environment.WebRootFileProvider ?? new NullFileProvider();
        _notFoundPage = _fileProvider.GetFileInfo("404.html");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldEvaluateRequest(context.Request))
        {
            await _next(context);
            return;
        }

        if (IsPublicRequest(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var routeExistence = DetermineRouteExistence(context.Request.Path);
        var isAuthenticated = context.User?.Identity?.IsAuthenticated ?? false;
        var isRootRequest = IsRootPath(context.Request.Path);

        if (isAuthenticated)
        {
            if (isRootRequest)
            {
                RedirectToAppHome(context);
                return;
            }

            if (routeExistence == RouteExistence.Missing)
            {
                await RespondNotFoundAsync(context);
                return;
            }

            await _next(context);
            return;
        }

        if (isRootRequest)
        {
            await _next(context);
            return;
        }

        if (routeExistence == RouteExistence.Missing)
        {
            await RespondNotFoundAsync(context);
            return;
        }

        RedirectToSignIn(context);
    }

    private static bool ShouldEvaluateRequest(HttpRequest request)
    {
        if (request is null)
        {
            return false;
        }

        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
        {
            return false;
        }

        if (request.Path.StartsWithSegments(ApiPathPrefix))
        {
            return false;
        }

        if (request.Path.StartsWithSegments(AzureSignInPath) || request.Path.StartsWithSegments(AzureSignInUrlPath))
        {
            return false;
        }

        if (UiRequestPath.HasValue && request.Path.StartsWithSegments(UiRequestPath))
        {
            return false;
        }

        if (request.Path.StartsWithSegments(NextAssetPrefix) || request.Path.StartsWithSegments(VercelAssetPrefix))
        {
            return false;
        }

        var value = request.Path.Value;
        if (!string.IsNullOrEmpty(value) && Path.HasExtension(value))
        {
            return false;
        }

        return true;
    }

    private static bool IsPublicRequest(PathString path)
    {
        if (!path.HasValue)
        {
            return false;
        }

        return path.StartsWithSegments(SignInPagePath)
            || path.StartsWithSegments(SignUpPagePath)
            || path.StartsWithSegments(LandingPagePath)
            || path.StartsWithSegments(SharePagePath);
    }

    private static bool IsRootPath(PathString path)
        => !path.HasValue || path == LandingRootPath;

    private RouteExistence DetermineRouteExistence(PathString path)
    {
        if (_fileProvider is NullFileProvider)
        {
            return RouteExistence.Unknown;
        }

        if (!path.HasValue || path == LandingRootPath)
        {
            return _fileProvider.GetFileInfo("index.html").Exists
                ? RouteExistence.Present
                : RouteExistence.Unknown;
        }

        var relativePath = path.Value!.Trim('/');

        if (string.IsNullOrEmpty(relativePath))
        {
            return _fileProvider.GetFileInfo("index.html").Exists
                ? RouteExistence.Present
                : RouteExistence.Unknown;
        }

        var normalized = relativePath.Replace('\\', '/');

        if (_fileProvider.GetFileInfo($"{normalized}.html").Exists)
        {
            return RouteExistence.Present;
        }

        if (_fileProvider.GetFileInfo($"{normalized}/index.html").Exists)
        {
            return RouteExistence.Present;
        }

        return RouteExistence.Missing;
    }

    private static string BuildRequestedTarget(PathString path, QueryString query)
    {
        if (!path.HasValue)
        {
            return "/";
        }

        return query.HasValue
            ? string.Concat(path.Value, query.Value)
            : path.Value!;
    }

    private void RedirectToAppHome(HttpContext context)
    {
        var normalized = EnsureTrailingSlash(new PathString(Program.MainAppPath));
        var redirect = UriHelper.BuildRelative(
            context.Request.PathBase,
            normalized,
            QueryString.Empty,
            FragmentString.Empty);

        _logger.LogDebug("Redirecting authenticated user from landing to {Destination}.", redirect);
        context.Response.Redirect(redirect, permanent: false);
    }

    private void RedirectToSignIn(HttpContext context)
    {
        var requestedTarget = BuildRequestedTarget(context.Request.Path, context.Request.QueryString);
        var resolvedTarget = AzureLoginRedirectHelper.ResolveRedirectPath(
            context,
            requestedTarget,
            Program.MainAppPath);

        var query = QueryString.FromUriComponent($"?redirectUri={Uri.EscapeDataString(resolvedTarget)}");
        var signInRedirect = UriHelper.BuildRelative(
            context.Request.PathBase,
            SignInUiPath,
            query,
            FragmentString.Empty);

        _logger.LogDebug(
            "Redirecting unauthenticated request for {RequestedPath} to sign-in page {Redirect}.",
            requestedTarget,
            signInRedirect);

        context.Response.Redirect(signInRedirect, permanent: false);
    }

    private async Task RespondNotFoundAsync(HttpContext context)
    {
        _logger.LogDebug(
            "Resolving request for {RequestedPath} to 404 because the route does not exist.",
            context.Request.Path);

        context.Response.StatusCode = StatusCodes.Status404NotFound;

        if (_notFoundPage.Exists)
        {
            context.Response.ContentType = "text/html; charset=utf-8";

            await using var stream = _notFoundPage.CreateReadStream();
            await stream.CopyToAsync(context.Response.Body);
        }
    }

    private static PathString EnsureTrailingSlash(PathString path)
    {
        if (!path.HasValue)
        {
            return new PathString("/");
        }

        return path.Value!.EndsWith('/', StringComparison.Ordinal)
            ? path
            : path.Add("/");
    }
}
