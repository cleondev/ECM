using System.Diagnostics;
using System.Globalization;
using System.Reflection;
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

using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Serilog;
using Serilog.Context;
using Serilog.Extensions.Hosting;

namespace ServiceDefaults;

public static class ServiceDefaultsExtensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.AddDefaultConfiguration();
        builder.AddDefaultHealthChecks();
        builder.AddCaching();

        builder.Services.AddHttpClient();
        builder.Services.AddHttpClient("resilient-test")
            .AddStandardResilienceHandler();

        var openTelemetryBuilder = builder.Services.AddOpenTelemetry();

        openTelemetryBuilder
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

        builder.AddSerilogLogging(openTelemetryBuilder);

        return builder;
    }

    public static IHostApplicationBuilder AddDefaultConfiguration(this IHostApplicationBuilder builder)
    {
        builder.Configuration.AddEnvironmentVariables();
        builder.Configuration.AddEnvironmentVariables(prefix: "ECM_");
        return builder;
    }

    public static IHostApplicationBuilder AddSerilogLogging(this IHostApplicationBuilder builder, OpenTelemetryBuilder? openTelemetryBuilder = null)
    {
        builder.Logging.ClearProviders();

        var fileSinkOptions = ResolveFileSinkOptions(builder.Configuration, builder.Environment);

        var loggerConfiguration = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
            .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName);

        if (!string.IsNullOrWhiteSpace(fileSinkOptions?.ServiceInstanceId))
        {
            loggerConfiguration = loggerConfiguration.Enrich.WithProperty("ServiceInstanceId", fileSinkOptions.ServiceInstanceId);
        }

        if (fileSinkOptions is not null)
        {
            loggerConfiguration = ConfigureFileSink(loggerConfiguration, fileSinkOptions);
        }

        loggerConfiguration = loggerConfiguration.WriteTo.Console();

        Log.Logger = loggerConfiguration.CreateLogger();

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

    private static IEnumerable<KeyValuePair<string, object>> CreateResourceAttributes(IHostEnvironment environment)
    {
        yield return new KeyValuePair<string, object>("deployment.environment", environment.EnvironmentName);
    }
    private static LoggerConfiguration ConfigureFileSink(LoggerConfiguration loggerConfiguration, FileSinkOptions options)
    {
        var outputTemplate = options.OutputTemplate
            ?? "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] ({Application}/{Environment}) [Trace:{TraceId}] [Corr:{CorrelationId}] [Req:{RequestId}] [User:{UserId}] {Message:lj}{NewLine}{Exception}";

        return loggerConfiguration.WriteTo.File(
            options.Path,
            outputTemplate: outputTemplate,
            rollingInterval: RollingInterval.Infinite,
            shared: true,
            retainedFileCountLimit: options.RetainedFileCountLimit,
            fileSizeLimitBytes: options.FileSizeLimitBytes,
            flushToDiskInterval: options.FlushToDiskInterval);
    }

    private static FileSinkOptions? ResolveFileSinkOptions(IConfiguration configuration, IHostEnvironment environment)
    {
        var section = configuration.GetSection("Serilog:File");

        if (!section.Exists())
        {
            return null;
        }

        var enabled = section.GetValue<bool?>("Enabled") ?? true;

        if (!enabled)
        {
            return null;
        }

        var baseDirectory = section.GetValue<string?>("Directory");
        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            baseDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        }

        var serviceName = section.GetValue<string?>("ServiceName")
            ?? configuration["Service:Name"]
            ?? environment.ApplicationName
            ?? Assembly.GetEntryAssembly()?.GetName().Name
            ?? "application";

        var serviceInstanceId = section.GetValue<string?>("ServiceInstanceId")
            ?? configuration["Service:InstanceId"]
            ?? configuration["SERVICE_INSTANCE_ID"]
            ?? Environment.GetEnvironmentVariable("SERVICE_INSTANCE_ID")
            ?? Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID")
            ?? Environment.MachineName;

        var postfix = section.GetValue<string?>("Postfix");
        postfix = string.IsNullOrWhiteSpace(postfix) ? "-log.txt" : postfix!;

        var dateFormat = section.GetValue<string?>("DateFormat");
        dateFormat = string.IsNullOrWhiteSpace(dateFormat) ? "dd/MM/yyyy" : dateFormat!;

        var rawFileName = $"{serviceName}:{serviceInstanceId}:{DateTime.UtcNow.ToString(dateFormat, CultureInfo.InvariantCulture)}{postfix}";
        var normalized = rawFileName.Replace('/', Path.DirectorySeparatorChar);

        var segments = normalized.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

        var sanitizedSegments = segments
            .Select(segment => SanitizePathSegment(segment))
            .ToArray();

        var combinedSegments = new string[sanitizedSegments.Length + 1];
        combinedSegments[0] = baseDirectory;
        Array.Copy(sanitizedSegments, 0, combinedSegments, 1, sanitizedSegments.Length);

        var fullPath = Path.Combine(combinedSegments);

        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var flushSeconds = section.GetValue<int?>("FlushToDiskIntervalSeconds");
        var retainedFiles = section.GetValue<int?>("RetainedFileCountLimit");
        var sizeLimit = section.GetValue<long?>("FileSizeLimitBytes");

        return new FileSinkOptions(
            fullPath,
            serviceInstanceId,
            section.GetValue<string?>("OutputTemplate"),
            retainedFiles,
            sizeLimit,
            flushSeconds.HasValue ? TimeSpan.FromSeconds(flushSeconds.Value) : null);
    }

    private static string SanitizePathSegment(string segment)
    {
        var invalidChars = Path.GetInvalidFileNameChars();

        foreach (var invalidChar in invalidChars)
        {
            if (!OperatingSystem.IsWindows() && invalidChar == ':')
            {
                continue;
            }

            segment = segment.Replace(invalidChar, '_');
        }

        return segment;
    }

    private sealed record FileSinkOptions(
        string Path,
        string ServiceInstanceId,
        string? OutputTemplate,
        int? RetainedFileCountLimit,
        long? FileSizeLimitBytes,
        TimeSpan? FlushToDiskInterval);
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
