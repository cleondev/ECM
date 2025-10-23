using System.IO;
using AppGateway.Api.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using ServiceDefaults;

namespace AppGateway.Api.Configuration;

public static class GatewayMiddlewareConfiguration
{
    public static WebApplication UseGatewayMiddleware(this WebApplication app)
    {
        app.UseSerilogEnrichedRequestLogging();
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

        return app;
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
            RequestPath = Program.UiRequestPath
        });

        app.UseStaticFiles();
        app.UseStaticFiles(new StaticFileOptions
        {
            RequestPath = Program.UiRequestPath
        });
    }
}
