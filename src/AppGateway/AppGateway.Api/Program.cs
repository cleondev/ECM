using System.IO;
using AppGateway.Api.Auth;
using AppGateway.Api.Middlewares;
using AppGateway.Api.ReverseProxy;
using AppGateway.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.StaticFiles;
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
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        });

        authenticationBuilder.AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
        authenticationBuilder.AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.AuthenticationScheme, _ => { });

        builder.Services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, ApiKeyAuthenticationHandler.AuthenticationScheme)
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
            app.UseDefaultFiles(new DefaultFilesOptions
            {
                RequestPath = uiRequestPath
            });

            app.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = uiRequestPath
            });
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        if (Directory.Exists(app.Environment.WebRootPath))
        {
            app.MapFallbackToFile($"{uiRequestPath}/{{*path}}", "index.html").AllowAnonymous();
        }

        app.MapGet("/", () => Results.Json(new
        {
            service = "app-gateway",
            status = "ready",
            routes = ReverseProxyConfiguration.CreateDefaultRoutes(app.Configuration)
        })).AllowAnonymous();

        app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();

        app.Run();
    }
}
