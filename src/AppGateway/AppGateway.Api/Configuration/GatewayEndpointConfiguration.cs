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

            return Results.Json(new
            {
                url = loginUrl
            });
        }).AllowAnonymous();

        app.MapGet("/signin-azure", (HttpContext context) =>
        {
            var redirectUri = AzureLoginRedirectHelper.ResolveRedirectPath(
                context,
                context.Request.Query["redirectUri"].FirstOrDefault(),
                Program.MainAppPath);

            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUri
            };

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

        var mainAppPath = Program.MainAppPath.TrimEnd('/');

        app.MapFallbackToFile($"{mainAppPath}/{{*path:nonfile}}", "app/index.html").AllowAnonymous();
        app.MapFallbackToFile(Program.MainAppPath, "app/index.html").AllowAnonymous();
        app.MapFallbackToFile($"{Program.UiRequestPath}/{{*path:nonfile}}", "index.html").AllowAnonymous();
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

}
