using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ServiceDefaults;

public static class ServiceDefaultsExtensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.AddDefaultConfiguration();
        builder.AddDefaultHealthChecks();

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
        builder.Configuration.AddEnvironmentVariables(prefix: "ECM_");
        return builder;
    }

    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy());
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
