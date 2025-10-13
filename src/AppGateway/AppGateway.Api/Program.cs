using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using AppGateway.Api.Auth;
using AppGateway.Api.Middlewares;
using AppGateway.Api.ReverseProxy;
using AppGateway.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Identity.Web;
using ServiceDefaults;
using Serilog;

namespace AppGateway.Api;

public static class Program
{
    public static void Main(string[] args)
    {
        var options = new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = Directory.GetCurrentDirectory(),
            WebRootPath = Directory.Exists(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "ui", "dist")))
                ? Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "ui", "dist"))
                : null  
        };

        var builder = WebApplication.CreateBuilder(options);

        builder.AddServiceDefaults();

        var authenticationBuilder = builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        });

        authenticationBuilder.AddMicrosoftIdentityWebApp(
            builder.Configuration.GetSection("AzureAd"),
            cookieScheme: CookieAuthenticationDefaults.AuthenticationScheme,
            openIdConnectScheme: OpenIdConnectDefaults.AuthenticationScheme);

        authenticationBuilder.AddMicrosoftIdentityWebApi(
            builder.Configuration.GetSection("AzureAd"),
            jwtBearerScheme: JwtBearerDefaults.AuthenticationScheme);

        authenticationBuilder.AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.AuthenticationScheme, _ => { });

        builder.Services.Configure<CookieAuthenticationOptions>(
            CookieAuthenticationDefaults.AuthenticationScheme,
            options =>
            {
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
            });

        builder.Services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(
                    JwtBearerDefaults.AuthenticationScheme,
                    ApiKeyAuthenticationHandler.AuthenticationScheme,
                    CookieAuthenticationDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build();
        });

        builder.Services.AddProblemDetails();
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddGatewayInfrastructure(builder.Configuration);
        builder.Services.AddScoped<IUserProvisioningService, AzureAdUserProvisioningService>();

        builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.Events ??= new OpenIdConnectEvents();
            var previousHandler = options.Events.OnTokenValidated;

            options.Events.OnTokenValidated = async context =>
            {
                if (previousHandler is not null)
                {
                    await previousHandler(context);
                }

                var provisioningService = context.HttpContext.RequestServices
                    .GetRequiredService<IUserProvisioningService>();

                await provisioningService.EnsureUserExistsAsync(
                    context.Principal,
                    context.HttpContext.RequestAborted);
            };
        });

        var app = builder.Build();

        app.UseSerilogRequestLogging();

        app.UseMiddleware<RequestLoggingMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        const string uiRequestPath = "/ecm";

        if (Directory.Exists(app.Environment.WebRootPath))
        {
            app.UseDefaultFiles();
            app.UseDefaultFiles(new DefaultFilesOptions
            {
                RequestPath = uiRequestPath
            });

            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = uiRequestPath
            });
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

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

        var homeTemplate = LoadTemplate(app.Environment.WebRootFileProvider, "home.html");

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

        if (Directory.Exists(app.Environment.WebRootPath))
        {
            app.MapFallbackToFile("index.html").AllowAnonymous();
            app.MapFallbackToFile($"{uiRequestPath}/{{*path}}", "index.html").AllowAnonymous();
        }

        app.MapGet("/service-status", () => Results.Json(new
        {
            service = "app-gateway",
            status = "ready",
            routes = ReverseProxyConfiguration.CreateDefaultRoutes(app.Configuration)
        })).AllowAnonymous();

        app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();

        app.Run();
    }

    private static string LoadTemplate(IFileProvider fileProvider, string fileName)
    {
        if (fileProvider is null)
        {
            throw new InvalidOperationException("Web root file provider is not available.");
        }

        var fileInfo = fileProvider.GetFileInfo(fileName);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException($"Could not locate the '{fileName}' template in the web root folder.");
        }

        using var stream = fileInfo.CreateReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: false);
        return reader.ReadToEnd();
    }
}
