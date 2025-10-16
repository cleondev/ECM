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

    public static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

        builder.Configuration
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true);

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
            "AccessControl",
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

            moduleDatabases[moduleDatabaseName] = builder.AddConnectionString(moduleDatabaseName);
        }

        var kafka = builder.AddConnectionString("kafka");
        var minio = builder.AddConnectionString("minio");

        var ecmUrl = builder.Configuration.GetValue<string>("Urls:Ecm") ?? "http://localhost:8080";
        var gatewayUrl = builder.Configuration.GetValue<string>("Urls:Gateway") ?? "http://localhost:5090";

        var ecmHost = builder.AddProject<Projects.ECM_Host>("ecm")
            .WithReference(kafka)
            .WithReference(minio)
            .WithEnvironment("ASPNETCORE_URLS", ecmUrl);

        foreach (var moduleDatabase in moduleDatabases.Values)
        {
            ecmHost = ecmHost.WithReference(moduleDatabase);
        }

        builder.AddProject<Projects.AppGateway_Api>("app-gateway")
            .WithReference(ecmHost)
            .WithEnvironment("ASPNETCORE_URLS", gatewayUrl)
            .WithEnvironment("Services__Ecm", ecmUrl);

        var searchIndexer = builder.AddProject<Projects.SearchIndexer>("search-indexer")
            .WithReference(kafka)
            .WithReference(ecmHost)
            .WithReference(moduleDatabases["Search"]);

        var outboxDispatcher = builder.AddProject<Projects.OutboxDispatcher>("outbox-dispatcher")
            .WithReference(kafka)
            .WithReference(ecmHost)
            .WithReference(moduleDatabases["Operations"]);

        if (connectionStrings.TryGetValue("Operations", out var operationsConnection))
        {
            outboxDispatcher = outboxDispatcher.WithEnvironment("ConnectionStrings__Operations", operationsConnection);
        }

        builder.AddProject<Projects.SearchIndexer>("notify")
            .WithReference(kafka);

        builder.Build().Run();
    }
}
