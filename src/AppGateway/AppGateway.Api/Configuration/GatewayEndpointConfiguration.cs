using System;
using System.IO;
using System.Linq;
using AppGateway.Api.ReverseProxy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace AppGateway.Api.Configuration;

public static class GatewayEndpointConfiguration
{
    public static WebApplication MapGatewayEndpoints(this WebApplication app)
    {
        app.MapControllers();
        ConfigureAuthenticationEndpoints(app);
        ConfigureFallbackEndpoints(app);
        ConfigureStatusEndpoints(app);

        return app;
    }

    private static void ConfigureAuthenticationEndpoints(WebApplication app)
    {
        app.MapGet("/signin-azure/url", (HttpContext context) =>
        {
            var redirectUri = ResolveRedirectPath(
                context.Request.Query["redirectUri"].FirstOrDefault(),
                Program.MainAppPath);

            var loginPath = QueryHelpers.AddQueryString("/signin-azure", "redirectUri", redirectUri);

            return Results.Json(new
            {
                url = loginPath
            });
        }).AllowAnonymous();

        app.MapGet("/signin-azure", (HttpContext context) =>
        {
            var redirectUri = ResolveRedirectPath(
                context.Request.Query["redirectUri"].FirstOrDefault(),
                Program.MainAppPath);

            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUri
            };

            return Results.Challenge(properties, new[] { OpenIdConnectDefaults.AuthenticationScheme });
        }).AllowAnonymous();

        app.MapPost("/signout", async (HttpContext context) =>
        {
            var redirectUri = ResolveRedirectPath(
                context.Request.Query["redirectUri"].FirstOrDefault(),
                Program.LandingPagePath,
                allowRoot: true);

            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUri
            };

            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, properties);

            return Results.Redirect(redirectUri);
        }).RequireAuthorization();
    }

    private static void ConfigureFallbackEndpoints(WebApplication app)
    {
        if (!Directory.Exists(app.Environment.WebRootPath))
        {
            return;
        }

        app.MapFallbackToFile("index.html").AllowAnonymous();
        app.MapFallbackToFile($"{Program.UiRequestPath}/{{*path}}", "index.html").AllowAnonymous();
    }

    private static void ConfigureStatusEndpoints(WebApplication app)
    {
        app.MapGet("/service-status", () => Results.Json(new
        {
            service = "app-gateway",
            status = "ready",
            routes = ReverseProxyConfiguration.CreateDefaultRoutes(app.Configuration)
        })).AllowAnonymous();

        app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();
    }

    private static string ResolveRedirectPath(string? candidate, string defaultPath, bool allowRoot = false)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return defaultPath;
        }

        if (!candidate.StartsWith('/', StringComparison.Ordinal))
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
}
