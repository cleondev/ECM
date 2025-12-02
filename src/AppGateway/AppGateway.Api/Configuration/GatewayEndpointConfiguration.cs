using System;
using System.IO;
using System.Linq;
using AppGateway.Api.Auth;
using AppGateway.Api.ReverseProxy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

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
            var redirectUri = AzureLoginRedirectHelper.ResolveRedirectPath(
                context,
                context.Request.Query["redirectUri"].FirstOrDefault(),
                Program.MainAppPath);

            var loginUrl = AzureLoginRedirectHelper.CreateLoginUrl(context, redirectUri);
            var silentLoginUrl = AzureLoginRedirectHelper.CreateLoginUrl(
                context,
                redirectUri,
                AzureLoginRedirectHelper.AzureLoginMode.Silent);

            return Results.Json(new
            {
                url = loginUrl,
                silentUrl = silentLoginUrl
            });
        }).AllowAnonymous();

        app.MapGet("/signin-azure", (HttpContext context) =>
        {
            var redirectUri = AzureLoginRedirectHelper.ResolveRedirectPath(
                context,
                context.Request.Query["redirectUri"].FirstOrDefault(),
                Program.MainAppPath);

            var loginMode = AzureLoginRedirectHelper.ResolveLoginMode(
                context.Request.Query["mode"].FirstOrDefault());

            var properties = AzureLoginRedirectHelper.CreateAuthenticationProperties(
                redirectUri,
                loginMode);

            return Results.Challenge(properties, [OpenIdConnectDefaults.AuthenticationScheme]);
        }).AllowAnonymous();

        app.MapPost("/signout", async (HttpContext context) =>
        {
            var redirectUri = AzureLoginRedirectHelper.ResolveRedirectPath(
                context,
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
        app.MapFallback("/api/{*path}", () => Results.NotFound()).AllowAnonymous();
        MapExportedRouteFallback(app, Program.MainAppPath, "app/index.html");
        MapExportedRouteFallback(app, "/me", "me/index.html");
        MapExportedRouteFallback(app, "/s", "s/index.html");
        MapExportedRouteFallback(app, "/settings", "settings/index.html");
        MapExportedRouteFallback(app, "/viewer", "viewer/index.html");
        MapExportedRouteFallback(app, "/organization-management", "organization-management/index.html");
        MapExportedRouteFallback(app, Program.UiRequestPath, "index.html");
        app.MapFallbackToFile("index.html").AllowAnonymous();
    }

    private static void ConfigureStatusEndpoints(WebApplication app)
    {
        app.MapGet("/service-status", () => Results.Json(new
        {
            service = "svc-app-gateway",
            status = "ready",
            routes = ReverseProxyConfiguration.CreateDefaultRoutes(app.Configuration)
        })).AllowAnonymous();

        app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();
    }

    private static void MapExportedRouteFallback(WebApplication app, string requestPath, string staticFile)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (string.IsNullOrWhiteSpace(app.Environment.WebRootPath))
        {
            return;
        }

        var filePath = Path.Combine(
            app.Environment.WebRootPath!,
            staticFile.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(filePath))
        {
            return;
        }

        var normalizedRequestPath = NormalizeRequestPath(requestPath);
        var nonFilePattern = normalizedRequestPath == "/"
            ? "{*path:nonfile}"
            : $"{normalizedRequestPath}/{{*path:nonfile}}";

        app.MapFallbackToFile(nonFilePattern, staticFile).AllowAnonymous();

        if (normalizedRequestPath != "/")
        {
            app.MapFallbackToFile(normalizedRequestPath, staticFile).AllowAnonymous();
        }
    }

    private static string NormalizeRequestPath(string requestPath)
    {
        if (string.IsNullOrWhiteSpace(requestPath))
        {
            return "/";
        }

        return requestPath.Length == 1 ? requestPath : requestPath.TrimEnd('/');
    }
}
