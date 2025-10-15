using ECM.BuildingBlocks.Infrastructure.Caching;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Serilog;
using Serilog.Extensions.Hosting;
using Serilog.Extensions.Logging;

namespace ServiceDefaults;

public static class ServiceDefaultsExtensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.AddDefaultConfiguration();
        builder.AddSerilogLogging();
        builder.AddDefaultHealthChecks();
        builder.AddCaching();

        builder.Services.AddHttpClient();
        builder.Services.AddHttpClient("resilient-test")
            .AddStandardResilienceHandler();

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName))
            .WithMetrics(metrics =>
            {
                metrics.AddMeter("Microsoft.AspNetCore.Hosting");
                metrics.AddMeter("System.Net.Http");
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource("System.Net.Http.HttpClient");
                tracing.AddOtlpExporter(options =>
                {
                });
            });

        return builder;
    }

    public static IHostApplicationBuilder AddDefaultConfiguration(this IHostApplicationBuilder builder)
    {
        builder.Configuration.AddEnvironmentVariables();
        builder.Configuration.AddEnvironmentVariables(prefix: "ECM_");
        return builder;
    }

    public static IHostApplicationBuilder AddSerilogLogging(this IHostApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
            .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
            .WriteTo.Console()
            .CreateLogger();

        builder.Logging.AddSerilog(Log.Logger, dispose: true);
        builder.Services.AddSingleton(sp => new DiagnosticContext(Log.Logger));

        return builder;
    }

    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy());
        return builder;
    }

    public static IHostApplicationBuilder AddCaching(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<CacheOptions>()
            .BindConfiguration(CacheOptions.SectionName);

        var cacheOptions = builder.Configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>() ?? new CacheOptions();

        builder.Services.AddConfiguredCache(cacheOptions);

        return builder;
    }
}

public static class ServiceDefaultsApplicationBuilderExtensions
{
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health");
        app.MapGet("/", () => Results.Ok(new { status = "Healthy" }));

        return app;
    }
}
