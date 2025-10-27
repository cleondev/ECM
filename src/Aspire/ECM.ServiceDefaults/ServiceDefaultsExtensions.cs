using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Claims;
using ECM.BuildingBlocks.Infrastructure.Caching;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Serilog;
using Serilog.Context;
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
            .ConfigureResource(resource =>
            {
                resource.AddService(builder.Environment.ApplicationName);
                resource.AddAttributes(CreateResourceAttributes(builder.Environment));
            })
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

        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeScopes = true;
            options.IncludeFormattedMessage = true;
            options.ParseStateValues = true;
            options.SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(builder.Environment.ApplicationName)
                .AddAttributes(CreateResourceAttributes(builder.Environment)));
            options.AddOtlpExporter();
        });

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

    private static IEnumerable<KeyValuePair<string, object?>> CreateResourceAttributes(IHostEnvironment environment)
    {
        yield return new KeyValuePair<string, object?>("deployment.environment", environment.EnvironmentName);
    }
}

public static class ServiceDefaultsApplicationBuilderExtensions
{
    public const string CorrelationIdHeaderName = "X-Correlation-ID";

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health");
        app.MapGet("/", () => Results.Ok(new { status = "Healthy" }));

        return app;
    }

    public static WebApplication UseSerilogEnrichedRequestLogging(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var correlationId = ResolveCorrelationId(context);

            if (!context.Response.Headers.ContainsKey(CorrelationIdHeaderName))
            {
                context.Response.Headers.TryAdd(CorrelationIdHeaderName, correlationId);
            }

            var correlationScope = LogContext.PushProperty("CorrelationId", correlationId);
            var requestScope = LogContext.PushProperty("RequestId", context.TraceIdentifier);
            IDisposable? traceScope = null;

            try
            {
                if (Activity.Current is { } activity)
                {
                    traceScope = LogContext.PushProperty("TraceId", activity.TraceId.ToString());
                }

                await next();
            }
            finally
            {
                traceScope?.Dispose();
                requestScope.Dispose();
                correlationScope.Dispose();
            }
        });

        app.UseSerilogRequestLogging(options =>
        {
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestId", httpContext.TraceIdentifier);

                var correlationId = ResolveCorrelationId(httpContext);
                diagnosticContext.Set("CorrelationId", correlationId);

                if (Activity.Current is { } activity)
                {
                    diagnosticContext.Set("TraceId", activity.TraceId.ToString());
                }

                var userId = httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? httpContext.User?.FindFirst("sub")?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    diagnosticContext.Set("UserId", userId);
                }
            };
        });

        return app;
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out StringValues headerValue)
            && !StringValues.IsNullOrEmpty(headerValue))
        {
            return headerValue.ToString();
        }

        if (Activity.Current is { } activity)
        {
            return activity.TraceId.ToString();
        }

        return context.TraceIdentifier;
    }
}
