using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;

namespace AppGateway.Api.Middlewares;

internal sealed class LandingRedirectMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LandingRedirectMiddleware> _logger;

    public LandingRedirectMiddleware(RequestDelegate next, ILogger<LandingRedirectMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldHandleRequest(context))
        {
            await _next(context);
            return;
        }

        var isAuthenticated = context.User?.Identity?.IsAuthenticated ?? false;

        if (!isAuthenticated)
        {
            var authenticateResult = await context.AuthenticateAsync();

            if (authenticateResult.Succeeded && authenticateResult.Principal is not null)
            {
                context.User = authenticateResult.Principal;
                isAuthenticated = authenticateResult.Principal.Identity?.IsAuthenticated ?? false;
            }
        }

        if (isAuthenticated)
        {
            var appPath = EnsureTrailingSlash(new PathString(Program.MainAppPath));
            var redirect = UriHelper.BuildRelative(
                context.Request.PathBase,
                appPath,
                QueryString.Empty,
                FragmentString.Empty);

            _logger.LogDebug("Redirecting authenticated landing request to {Destination}.", redirect);
            context.Response.Redirect(redirect, permanent: false);
            return;
        }

        await _next(context);
    }

    private static bool ShouldHandleRequest(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
        {
            return false;
        }

        var path = context.Request.Path;
        return !path.HasValue || path == "/";
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
