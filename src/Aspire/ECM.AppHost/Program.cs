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

        var environmentName = GetEnvironmentName();
        ConfigureConfiguration(builder, environmentName);
        ConfigureDashboard(builder);

        var connectionStrings = LoadConnectionStrings(builder.Configuration);
        var moduleDatabaseNames = GetModuleDatabaseNames();

        var prefixedConnectionStrings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        var moduleDatabaseResourceNames = ConfigureModuleDatabaseResources(
            moduleDatabaseNames,
            connectionStrings,
            prefixedConnectionStrings);

        var (kafkaResourceName, minioResourceName) = ConfigureExternalServiceResources(
            connectionStrings,
            prefixedConnectionStrings);

        ApplyPrefixedConnectionStrings(builder, prefixedConnectionStrings);

        var moduleDatabases = CreateModuleDatabaseResources(builder, moduleDatabaseResourceNames);
        var kafka = builder.AddConnectionString(kafkaResourceName);
        var minio = builder.AddConnectionString(minioResourceName);

        var (ecmUri, gatewayUri) = GetApplicationUris(builder.Configuration);

        var ecmHost = ConfigureEcmHost(builder, moduleDatabases, kafka, minio, ecmUri);
        ConfigureGateway(builder, ecmHost, ecmUri, gatewayUri);

        ConfigureWorkers(builder, builder.Configuration, connectionStrings, moduleDatabases, kafka, ecmHost);

        builder.Build().Run();
    }

    #region Configuration & Dashboard

    private static string GetEnvironmentName()
    {
        return Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
    }

    private static void ConfigureConfiguration(IDistributedApplicationBuilder builder, string environmentName)
    {
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true);

        if (string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase))
        {
            builder.Configuration.AddUserSecrets(typeof(Program).Assembly, optional: true);
        }

        builder.Configuration.AddEnvironmentVariables();
    }

    private static void ConfigureDashboard(IDistributedApplicationBuilder builder)
    {
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
    }

    private static IReadOnlyDictionary<string, string?> LoadConnectionStrings(IConfiguration configuration)
    {
        return configuration
            .GetSection("ConnectionStrings")
            .GetChildren()
            .ToDictionary(
                section => section.Key,
                section => section.Value,
                StringComparer.OrdinalIgnoreCase);
    }

    private static string[] GetModuleDatabaseNames()
    {
        return new[]
        {
            "IAM",
            "Document",
            "File",
            "Workflow",
            "Search",
            "Ocr",
            "Operations",
            "Webhook"
        };
    }

    #endregion

    #region Database & External Services

    private static Dictionary<string, string> ConfigureModuleDatabaseResources(
        IEnumerable<string> moduleDatabaseNames,
        IReadOnlyDictionary<string, string?> connectionStrings,
        IDictionary<string, string?> prefixedConnectionStrings)
    {
        var moduleDatabaseResourceNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var moduleDatabaseName in moduleDatabaseNames)
        {
            if (!connectionStrings.TryGetValue(moduleDatabaseName, out var connectionString)
                || string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"Connection string '{moduleDatabaseName}' must be configured in the Aspire AppHost before starting the application.");
            }

            var databaseResourceName = BuildResourceName(DatabaseResourcePrefix, moduleDatabaseName);
            moduleDatabaseResourceNames[moduleDatabaseName] = databaseResourceName;
            prefixedConnectionStrings[$"ConnectionStrings:{databaseResourceName}"] = connectionString;
        }

        return moduleDatabaseResourceNames;
    }

    private static (string kafkaResourceName, string minioResourceName) ConfigureExternalServiceResources(
        IReadOnlyDictionary<string, string?> connectionStrings,
        IDictionary<string, string?> prefixedConnectionStrings)
    {
        var kafkaResourceName = BuildResourceName(ServiceResourcePrefix, "kafka");
        var minioResourceName = BuildResourceName(ServiceResourcePrefix, "minio");

        if (!connectionStrings.TryGetValue("kafka", out var kafkaConnectionString)
            || string.IsNullOrWhiteSpace(kafkaConnectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'kafka' must be configured in the Aspire AppHost before starting the application.");
        }

        if (!connectionStrings.TryGetValue("minio", out var minioConnectionString)
            || string.IsNullOrWhiteSpace(minioConnectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'minio' must be configured in the Aspire AppHost before starting the application.");
        }

        prefixedConnectionStrings[$"ConnectionStrings:{kafkaResourceName}"] = kafkaConnectionString;
        prefixedConnectionStrings[$"ConnectionStrings:{minioResourceName}"] = minioConnectionString;

        return (kafkaResourceName, minioResourceName);
    }

    private static void ApplyPrefixedConnectionStrings(
        IDistributedApplicationBuilder builder,
        IDictionary<string, string?> prefixedConnectionStrings)
    {
        if (prefixedConnectionStrings.Count == 0)
        {
            return;
        }

        builder.Configuration.AddInMemoryCollection(prefixedConnectionStrings);
    }

    private static Dictionary<string, IResourceBuilder<IResourceWithConnectionString>> CreateModuleDatabaseResources(
        IDistributedApplicationBuilder builder,
        IDictionary<string, string> moduleDatabaseResourceNames)
    {
        var moduleDatabases = new Dictionary<string, IResourceBuilder<IResourceWithConnectionString>>(StringComparer.OrdinalIgnoreCase);

        foreach (var moduleDatabase in moduleDatabaseResourceNames)
        {
            moduleDatabases[moduleDatabase.Key] = builder.AddConnectionString(moduleDatabase.Value);
        }

        return moduleDatabases;
    }

    #endregion

    #region Applications (ECM + Gateway)

    private static (Uri ecmUri, Uri gatewayUri) GetApplicationUris(IConfiguration configuration)
    {
        var ecmUrl = configuration.GetValue<string>("Urls:Ecm") ?? string.Empty;
        var gatewayUrl = configuration.GetValue<string>("Urls:Gateway") ?? string.Empty;
        var ecmUri = EnsureHttpUri(ecmUrl, "Urls:Ecm");
        var gatewayUri = EnsureHttpUri(gatewayUrl, "Urls:Gateway");

        return (ecmUri, gatewayUri);
    }

    private static IResourceBuilder<ProjectResource> ConfigureEcmHost(
        IDistributedApplicationBuilder builder,
        IDictionary<string, IResourceBuilder<IResourceWithConnectionString>> moduleDatabases,
        IResourceBuilder<IResourceWithConnectionString> kafka,
        IResourceBuilder<IResourceWithConnectionString> minio,
        Uri ecmUri)
    {
        var ecmResourceName = BuildResourceName(AppResourcePrefix, "ecm");

        var ecmHost = builder.AddProject<Projects.ECM_Host>(ecmResourceName)
            .WithReference(kafka)
            .WithReference(minio);

        foreach (var moduleDatabase in moduleDatabases.Values)
        {
            ecmHost = ecmHost.WithReference(moduleDatabase);
        }

        ConfigureProjectResource(ecmHost, ecmUri, ecmResourceName);

        return ecmHost;
    }

    private static void ConfigureGateway(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> ecmHost,
        Uri ecmUri,
        Uri gatewayUri)
    {
        var gatewayResourceName = BuildResourceName(ServiceResourcePrefix, "app-gateway");

        var appGateway = builder.AddProject<Projects.AppGateway_Api>(gatewayResourceName)
            .WithReference(ecmHost)
            .WithEnvironment("Services__Ecm", ecmUri.ToString());

        ConfigureProjectResource(appGateway, gatewayUri, gatewayResourceName);
    }

    #endregion

    private static void ConfigureWorkers(
        IDistributedApplicationBuilder builder,
        IConfiguration configuration,
        IReadOnlyDictionary<string, string?> connectionStrings,
        IDictionary<string, IResourceBuilder<IResourceWithConnectionString>> moduleDatabases,
        IResourceBuilder<IResourceWithConnectionString> kafka,
        IResourceBuilder<ProjectResource> ecmHost)
    {
        ConfigureSearchIndexerWorker(builder, moduleDatabases, kafka, ecmHost);
        ConfigureOcrWorker(builder, configuration, connectionStrings, kafka, ecmHost);
        ConfigureOutboxDispatcherWorker(builder, connectionStrings, moduleDatabases, kafka, ecmHost);
        ConfigureWebhookDispatcherWorker(builder, moduleDatabases, kafka);
        ConfigureTaggerWorker(builder, kafka);
        ConfigureNotifyWorker(builder, kafka);
    }

    private static void ConfigureSearchIndexerWorker(
        IDistributedApplicationBuilder builder,
        IDictionary<string, IResourceBuilder<IResourceWithConnectionString>> moduleDatabases,
        IResourceBuilder<IResourceWithConnectionString> kafka,
        IResourceBuilder<ProjectResource> ecmHost)
    {
        var searchIndexerResourceName = BuildResourceName(WorkerResourcePrefix, "search-indexer");

        builder.AddProject<Projects.SearchIndexer>(searchIndexerResourceName)
            .WithReference(kafka)
            .WithReference(ecmHost)
            .WithReference(moduleDatabases["Search"]);
    }

    private static void ConfigureOcrWorker(
        IDistributedApplicationBuilder builder,
        IConfiguration configuration,
        IReadOnlyDictionary<string, string?> connectionStrings,
        IResourceBuilder<IResourceWithConnectionString> kafka,
        IResourceBuilder<ProjectResource> ecmHost)
    {
        var ocrWorkerResourceName = BuildResourceName(WorkerResourcePrefix, "ocr");

        var ocrWorker = builder.AddProject<Projects.Ocr>(ocrWorkerResourceName)
            .WithReference(kafka)
            .WithReference(ecmHost);

        if (connectionStrings.TryGetValue("Ocr", out var ocrConnection)
            && !string.IsNullOrWhiteSpace(ocrConnection))
        {
            ocrWorker.WithEnvironment("ConnectionStrings__Ocr", ocrConnection);
        }

        var dotOcrConfiguration = configuration.GetSection("Ocr:Dot");
        foreach (var setting in dotOcrConfiguration.GetChildren())
        {
            var key = $"Ocr__Dot__{setting.Key}";
            ocrWorker.WithEnvironment(key, setting.Value);
        }
    }

    private static void ConfigureOutboxDispatcherWorker(
        IDistributedApplicationBuilder builder,
        IReadOnlyDictionary<string, string?> connectionStrings,
        IDictionary<string, IResourceBuilder<IResourceWithConnectionString>> moduleDatabases,
        IResourceBuilder<IResourceWithConnectionString> kafka,
        IResourceBuilder<ProjectResource> ecmHost)
    {
        var outboxDispatcherResourceName = BuildResourceName(WorkerResourcePrefix, "outbox-dispatcher");

        var outboxDispatcher = builder.AddProject<Projects.OutboxDispatcher>(outboxDispatcherResourceName)
            .WithReference(kafka)
            .WithReference(ecmHost)
            .WithReference(moduleDatabases["Operations"]);

        if (connectionStrings.TryGetValue("Operations", out var operationsConnection)
            && !string.IsNullOrWhiteSpace(operationsConnection))
        {
            outboxDispatcher.WithEnvironment("ConnectionStrings__Operations", operationsConnection);
        }
    }

    private static void ConfigureWebhookDispatcherWorker(
        IDistributedApplicationBuilder builder,
        IDictionary<string, IResourceBuilder<IResourceWithConnectionString>> moduleDatabases,
        IResourceBuilder<IResourceWithConnectionString> kafka)
    {
        var webhookDispatcherResourceName = BuildResourceName(WorkerResourcePrefix, "webhook");

        builder.AddProject<Projects.WebhookDispatcher>(webhookDispatcherResourceName)
            .WithReference(kafka)
            .WithReference(moduleDatabases["Webhook"]);
    }

    private static void ConfigureTaggerWorker(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithConnectionString> kafka)
    {
        var taggerResourceName = BuildResourceName(WorkerResourcePrefix, "tagger");

        builder.AddProject<Projects.Tagger>(taggerResourceName)
            .WithReference(kafka);
    }

    private static void ConfigureNotifyWorker(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithConnectionString> kafka)
    {
        var notifyResourceName = BuildResourceName(WorkerResourcePrefix, "notify");

        builder.AddProject<Projects.Notify>(notifyResourceName)
            .WithReference(kafka);
    }


    private static Uri EnsureHttpUri(string value, string settingName)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException(
                $"The configured URL '{value}' for '{settingName}' must be an absolute URI.");
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"The configured URL '{value}' for '{settingName}' must use HTTP or HTTPS.");
        }

        return uri;
    }

    private static void ConfigureProjectResource(
        IResourceBuilder<ProjectResource> builder,
        Uri uri,
        string endpointBaseName)
    {
        var port = GetEffectivePort(uri);

        RemoveHttpAndHttpsEndpoints(builder);

        if (string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            builder.WithHttpsEndpoint(
                port: port,
                targetPort: port,
                name: $"{endpointBaseName}-https",
                isProxied: false);
        }
        else
        {
            builder.WithHttpEndpoint(
                port: port,
                targetPort: port,
                name: $"{endpointBaseName}-http",
                isProxied: false);
        }

        builder.WithEnvironment("ASPNETCORE_URLS", BuildBindingUrl(uri));
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
            .Where(annotation =>
                string.Equals(annotation.UriScheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
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
