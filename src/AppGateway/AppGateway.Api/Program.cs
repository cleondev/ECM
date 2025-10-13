using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
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

        authenticationBuilder.AddCookie(options =>
        {
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
        });

        authenticationBuilder.AddMicrosoftIdentityWebApp(
            builder.Configuration.GetSection("AzureAd"),
            cookieScheme: CookieAuthenticationDefaults.AuthenticationScheme,
            openIdConnectScheme: OpenIdConnectDefaults.AuthenticationScheme);

        authenticationBuilder.AddMicrosoftIdentityWebApi(
            builder.Configuration.GetSection("AzureAd"),
            jwtBearerScheme: JwtBearerDefaults.AuthenticationScheme);

        authenticationBuilder.AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.AuthenticationScheme, _ => { });

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

            if (string.IsNullOrWhiteSpace(redirectUri) || !redirectUri.StartsWith('/', StringComparison.Ordinal))
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

            if (string.IsNullOrWhiteSpace(redirectUri) || !redirectUri.StartsWith('/', StringComparison.Ordinal))
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

            var html = $"""<!DOCTYPE html>
<html lang=\"vi\">
<head>
    <meta charset=\"utf-8\" />
    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />
    <title>ECM · Trang chủ</title>
    <style>
        :root {
            color-scheme: dark;
            font-family: 'Inter', system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
            background: radial-gradient(circle at 15% 20%, rgba(37, 99, 235, 0.14), transparent 52%),
                        radial-gradient(circle at 80% 0%, rgba(236, 72, 153, 0.12), transparent 55%),
                        #050816;
            color: #f8fafc;
        }

        * {
            box-sizing: border-box;
        }

        body {
            margin: 0;
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 2.5rem 1.5rem;
        }

        main {
            width: min(640px, 100%);
            display: grid;
            gap: 1.5rem;
            padding: 2.75rem;
            border-radius: 28px;
            background: rgba(15, 23, 42, 0.72);
            border: 1px solid rgba(148, 163, 184, 0.18);
            backdrop-filter: blur(14px);
            text-align: center;
        }

        h1 {
            margin: 0;
            font-size: 2.2rem;
            letter-spacing: 0.08em;
            text-transform: uppercase;
        }

        .subtitle {
            margin: 0.4rem 0 0;
            color: rgba(226, 232, 240, 0.78);
            font-size: 0.95rem;
        }

        p {
            margin: 0;
            font-size: 1rem;
            color: rgba(203, 213, 225, 0.92);
        }

        form {
            margin-top: 1.5rem;
        }

        button {
            font-family: inherit;
            font-weight: 600;
            font-size: 0.95rem;
            padding: 0.9rem 1.4rem;
            border-radius: 14px;
            border: none;
            cursor: pointer;
            background: linear-gradient(135deg, #ef4444, #f97316);
            color: #f8fafc;
            display: inline-flex;
            align-items: center;
            justify-content: center;
            gap: 0.6rem;
            transition: transform 0.2s ease, box-shadow 0.2s ease;
        }

        button:hover {
            transform: translateY(-2px);
            box-shadow: 0 12px 32px rgba(249, 115, 22, 0.35);
        }

        small {
            color: rgba(148, 163, 184, 0.8);
        }
    </style>
</head>
<body>
    <main>
        <section>
            <h1>Chào mừng, {name}</h1>
            {subtitle}
        </section>
        <section>
            <p>Bạn đã đăng nhập thành công vào hệ thống ECM thông qua Azure Active Directory.</p>
            <p>Hãy tiếp tục khám phá các tính năng hoặc đăng xuất khỏi phiên làm việc của bạn.</p>
        </section>
        <form method=\"post\" action=\"/signout\">
            <input type=\"hidden\" name=\"redirectUri\" value=\"/\" />
            <button type=\"submit\">Đăng xuất</button>
        </form>
        <small>Phiên đăng nhập sẽ tự động hết hạn sau 8 giờ không hoạt động.</small>
    </main>
</body>
</html>""";

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
}
