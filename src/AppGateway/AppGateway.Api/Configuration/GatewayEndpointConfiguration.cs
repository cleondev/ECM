using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using AppGateway.Api.ReverseProxy;
using AppGateway.Api.Templates;
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
        ConfigureHomeEndpoint(app);
        ConfigureFallbackEndpoints(app);
        ConfigureStatusEndpoints(app);

        return app;
    }

    private static void ConfigureAuthenticationEndpoints(WebApplication app)
    {
        app.MapGet("/signin-azure/url", (HttpContext context) =>
        {
            var redirectUri = context.Request.Query["redirectUri"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(redirectUri) || !redirectUri.StartsWith("/", StringComparison.Ordinal))
            {
                redirectUri = "/home";
            }

            var loginPath = QueryHelpers.AddQueryString("/signin-azure", "redirectUri", redirectUri);

            return Results.Json(new
            {
                url = loginPath
            });
        }).AllowAnonymous();

        app.MapGet("/signin-azure", (HttpContext context) =>
        {
            var redirectUri = context.Request.Query["redirectUri"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(redirectUri))
            {
                redirectUri = "/home";
            }

            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUri
            };

            return Results.Challenge(properties, new[] { OpenIdConnectDefaults.AuthenticationScheme });
        }).AllowAnonymous();

        app.MapPost("/signout", async (HttpContext context) =>
        {
            var redirectUri = context.Request.Query["redirectUri"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(redirectUri) || !redirectUri.StartsWith("/", StringComparison.Ordinal))
            {
                redirectUri = "/";
            }

            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUri
            };

            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, properties);

            return Results.Redirect(redirectUri);
        }).RequireAuthorization();
    }

    private static void ConfigureHomeEndpoint(WebApplication app)
    {
        var homeTemplate = HomeTemplateProvider.Load(app.Environment.WebRootFileProvider, "home.html");

        app.MapGet("/home", (HttpContext context) =>
        {
            var user = context.User;
            var name = user?.FindFirst("name")?.Value
                       ?? user?.Identity?.Name
                       ?? "bạn";
            var email = user?.FindFirst(ClaimTypes.Email)?.Value
                        ?? user?.FindFirst("preferred_username")?.Value
                        ?? string.Empty;

            name = WebUtility.HtmlEncode(name);
            email = WebUtility.HtmlEncode(email);

            var subtitle = string.IsNullOrEmpty(email)
                ? string.Empty
                : $"<p class=\"subtitle\">{email}</p>";

            var html = homeTemplate
                .Replace("{{NAME}}", name, StringComparison.Ordinal)
                .Replace("{{SUBTITLE}}", subtitle, StringComparison.Ordinal);

            return Results.Content(html, "text/html; charset=utf-8");
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
}
