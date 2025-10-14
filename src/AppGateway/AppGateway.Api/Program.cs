using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using AppGateway.Api.Auth;
using AppGateway.Api.Middlewares;
using AppGateway.Api.ReverseProxy;
using AppGateway.Api.Templates;
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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using ServiceDefaults;
using Serilog;

namespace AppGateway.Api;

public static class Program
{
    private const string UiRequestPath = "/ecm";

    public static void Main(string[] args)
    {
        var builder = CreateBuilder(args);

        ConfigureServices(builder);

        var app = builder.Build();

        ConfigureMiddleware(app);
        ConfigureEndpoints(app);

        app.Run();
    }

    private static WebApplicationBuilder CreateBuilder(string[] args)
    {
        var options = new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = Directory.GetCurrentDirectory(),
            WebRootPath = ResolveWebRootPath()
        };

        var builder = WebApplication.CreateBuilder(options);

        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddUserSecrets(typeof(Program).Assembly, optional: true);
        }

        builder.Configuration.AddEnvironmentVariables();
        builder.AddServiceDefaults();

        return builder;
    }

    private static string? ResolveWebRootPath()
    {
        var path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "ui", "dist"));
        return Directory.Exists(path) ? path : null;
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        ConfigureAuthentication(builder);

        builder.Services.AddProblemDetails();
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddGatewayInfrastructure(builder.Configuration);
        builder.Services.AddScoped<IUserProvisioningService, AzureAdUserProvisioningService>();

        builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.UsePkce = true;
            options.ResponseMode = OpenIdConnectResponseMode.FormPost;
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
    }

    private static void ConfigureAuthentication(WebApplicationBuilder builder)
    {
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
    }

    private static void ConfigureMiddleware(WebApplication app)
    {
        app.UseSerilogRequestLogging();
        app.UseMiddleware<RequestLoggingMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        ConfigureStaticFileServing(app);

        app.UseAuthentication();
        app.UseAuthorization();
    }

    private static void ConfigureStaticFileServing(WebApplication app)
    {
        if (!Directory.Exists(app.Environment.WebRootPath))
        {
            return;
        }

        app.UseDefaultFiles();
        app.UseDefaultFiles(new DefaultFilesOptions
        {
            RequestPath = UiRequestPath
        });

        app.UseStaticFiles();
        app.UseStaticFiles(new StaticFileOptions
        {
            RequestPath = UiRequestPath
        });
    }

    private static void ConfigureEndpoints(WebApplication app)
    {
        app.MapControllers();
        ConfigureAuthenticationEndpoints(app);
        ConfigureHomeEndpoint(app);
        ConfigureFallbackEndpoints(app);
        ConfigureStatusEndpoints(app);
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
        app.MapFallbackToFile($"{UiRequestPath}/{{*path}}", "index.html").AllowAnonymous();
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
