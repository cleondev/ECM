using System;
using System.IO;
using AppGateway.Api.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using ServiceDefaults;

namespace AppGateway.Api;

public static class Program
{
    internal const string UiRequestPath = "/ecm";

    public static void Main(string[] args)
    {
        var builder = CreateBuilder(args);

        builder.ConfigureGatewayServices();

        var app = builder.Build();

        app.UseGatewayMiddleware();
        app.MapGatewayEndpoints();

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
}
