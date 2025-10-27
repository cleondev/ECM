using System;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace AppHost;

public static class Program
{
    private const string DashboardUrlVariable = "ASPNETCORE_URLS";
    private const string DashboardGrpcVariable = "ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL";
    private const string DashboardHttpVariable = "ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL";
    private const string AllowUnsecuredTransportVariable = "ASPIRE_ALLOW_UNSECURED_TRANSPORT";
    private const string DashboardUnsecured = "ASPIRE_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS";

    private const string DatabaseResourcePrefix = "db-";
    private const string AppResourcePrefix = "app-";
    private const string ServiceResourcePrefix = "svc-";
    private const string WorkerResourcePrefix = "worker-";

    public static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

        builder.Configuration
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true);

        if (string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase))
        {
            builder.Configuration.AddUserSecrets(typeof(Program).Assembly, optional: true);
        }

        builder.Configuration.AddEnvironmentVariables();

        var dashboardDefaults = new Dictionary<string, string?>();

        if (string.IsNullOrWhiteSpace(builder.Configuration[DashboardUrlVariable]))
        {
            dashboardDefaults[DashboardUrlVariable] = "http://localhost:18888";
        }

        if (string.IsNullOrWhiteSpace(builder.Configuration[DashboardGrpcVariable])
            && string.IsNullOrWhiteSpace(builder.Configuration[DashboardHttpVariable]))
        {
            dashboardDefaults[DashboardHttpVariable] = "http://localhost:4318";
        }

        if (string.IsNullOrWhiteSpace(builder.Configuration[DashboardUnsecured]))
        {
            dashboardDefaults[DashboardUnsecured] = "true";
        }

        if (string.IsNullOrWhiteSpace(builder.Configuration[AllowUnsecuredTransportVariable]))
        {
            dashboardDefaults[AllowUnsecuredTransportVariable] = "true";
        }

        if (dashboardDefaults.Count > 0)
        {
            builder.Configuration.AddInMemoryCollection(dashboardDefaults);
        }

        var moduleDatabaseNames = new[]
        {
            "IAM",
            "Document",
            "File",
            "Workflow",
            "Search",
            "Ocr",
            "Operations"
        };

        var moduleDatabases = new Dictionary<string, IResourceBuilder<IResourceWithConnectionString>>(StringComparer.OrdinalIgnoreCase);

        var connectionStrings = builder.Configuration
            .GetSection("ConnectionStrings")
            .GetChildren()
            .ToDictionary(section => section.Key, section => section.Value, StringComparer.OrdinalIgnoreCase);

        foreach (var moduleDatabaseName in moduleDatabaseNames)
        {
            var connectionString = builder.Configuration.GetConnectionString(moduleDatabaseName);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"Connection string '{moduleDatabaseName}' must be configured in the Aspire AppHost before starting the application.");
            }

            var databaseResourceName = BuildResourceName(DatabaseResourcePrefix, moduleDatabaseName);
            moduleDatabases[moduleDatabaseName] = builder.AddConnectionString(databaseResourceName);
        }

        var kafka = builder.AddConnectionString(BuildResourceName(ServiceResourcePrefix, "kafka"));
        var minio = builder.AddConnectionString(BuildResourceName(ServiceResourcePrefix, "minio"));

        var ecmUrl = builder.Configuration.GetValue<string>("Urls:Ecm") ?? "http://localhost:8080";
        var gatewayUrl = builder.Configuration.GetValue<string>("Urls:Gateway") ?? "http://localhost:5090";

        var ecmUri = EnsureHttpUri(ecmUrl, "Urls:Ecm");
        var gatewayUri = EnsureHttpUri(gatewayUrl, "Urls:Gateway");

        var ecmResourceName = BuildResourceName(AppResourcePrefix, "ecm");
        var ecmHost = builder.AddProject<Projects.ECM_Host>(ecmResourceName)
            .WithReference(kafka)
            .WithReference(minio);

        foreach (var moduleDatabase in moduleDatabases.Values)
        {
            ecmHost = ecmHost.WithReference(moduleDatabase);
        }

        ecmHost = ConfigureProjectResource(ecmHost, ecmUri, ecmResourceName);

        var gatewayResourceName = BuildResourceName(ServiceResourcePrefix, "app-gateway");
        var appGateway = builder.AddProject<Projects.AppGateway_Api>(gatewayResourceName)
            .WithReference(ecmHost)
            .WithEnvironment("Services__Ecm", ecmUri.ToString());

        appGateway = ConfigureProjectResource(appGateway, gatewayUri, gatewayResourceName);

        var searchIndexerResourceName = BuildResourceName(WorkerResourcePrefix, "search-indexer");
        var searchIndexer = builder.AddProject<Projects.SearchIndexer>(searchIndexerResourceName)
            .WithReference(kafka)
            .WithReference(ecmHost)
            .WithReference(moduleDatabases["Search"]);

        var ocrWorkerResourceName = BuildResourceName(WorkerResourcePrefix, "ocr");
        var ocrWorker = builder.AddProject<Projects.Ocr>(ocrWorkerResourceName)
            .WithReference(kafka)
            .WithReference(ecmHost);

        if (connectionStrings.TryGetValue("Ocr", out var ocrConnection))
        {
            ocrWorker = ocrWorker.WithEnvironment("ConnectionStrings__Ocr", ocrConnection);
        }

        var dotOcrConfiguration = builder.Configuration.GetSection("Ocr:Dot");
        foreach (var setting in dotOcrConfiguration.GetChildren())
        {
            var key = $"Ocr__Dot__{setting.Key}";
            ocrWorker = ocrWorker.WithEnvironment(key, setting.Value);
        }

        var outboxDispatcherResourceName = BuildResourceName(WorkerResourcePrefix, "outbox-dispatcher");
        var outboxDispatcher = builder.AddProject<Projects.OutboxDispatcher>(outboxDispatcherResourceName)
            .WithReference(kafka)
            .WithReference(ecmHost)
            .WithReference(moduleDatabases["Operations"]);

        if (connectionStrings.TryGetValue("Operations", out var operationsConnection))
        {
            outboxDispatcher = outboxDispatcher.WithEnvironment("ConnectionStrings__Operations", operationsConnection);
        }

        var notifyResourceName = BuildResourceName(WorkerResourcePrefix, "notify");
        builder.AddProject<Projects.SearchIndexer>(notifyResourceName)
            .WithReference(kafka);

        builder.Build().Run();
    }

    private static Uri EnsureHttpUri(string value, string settingName)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException($"The configured URL '{value}' for '{settingName}' must be an absolute URI.");
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"The configured URL '{value}' for '{settingName}' must use HTTP or HTTPS.");
        }

        return uri;
    }

    private static IResourceBuilder<ProjectResource> ConfigureProjectResource(
        IResourceBuilder<ProjectResource> builder,
        Uri uri,
        string endpointBaseName)
    {
        var port = GetEffectivePort(uri);

        RemoveHttpAndHttpsEndpoints(builder);

        if (string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            builder = builder.WithHttpsEndpoint(
                port: port,
                targetPort: port,
                name: $"{endpointBaseName}-https",
                isProxied: false);
        }
        else
        {
            builder = builder.WithHttpEndpoint(
                port: port,
                targetPort: port,
                name: $"{endpointBaseName}-http",
                isProxied: false);
        }

        builder = builder.WithEnvironment("ASPNETCORE_URLS", BuildBindingUrl(uri));

        return builder;
    }

    private static string BuildBindingUrl(Uri uri)
    {
        var port = GetEffectivePort(uri);
        return $"{uri.Scheme}://0.0.0.0:{port}";
    }

    private static int GetEffectivePort(Uri uri)
    {
        if (!uri.IsDefaultPort)
        {
            return uri.Port;
        }

        return string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            ? 443
            : 80;
    }

    private static void RemoveHttpAndHttpsEndpoints(IResourceBuilder<ProjectResource> builder)
    {
        var projectResource = builder.Resource;

        var endpointsToRemove = projectResource.Annotations
            .OfType<EndpointAnnotation>()
            .Where(annotation => string.Equals(annotation.UriScheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || string.Equals(annotation.UriScheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (endpointsToRemove.Count == 0)
        {
            return;
        }

        foreach (var annotation in endpointsToRemove)
        {
            projectResource.Annotations.Remove(annotation);
        }
    }

    private static string BuildResourceName(string prefix, string name)
    {
        return prefix + name.ToLowerInvariant();
    }
}
