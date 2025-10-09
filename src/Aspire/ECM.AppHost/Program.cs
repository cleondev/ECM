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

        var postgres = builder.AddConnectionString("postgres");
        var kafka = builder.AddConnectionString("kafka");
        var minio = builder.AddConnectionString("minio");

        var ecmHost = builder.AddProject<Projects.ECM_Host>("ecm")
            .WithReference(postgres)
            .WithReference(kafka)
            .WithReference(minio);

        builder.AddProject<Projects.Workflow>("workflow")
            .WithReference(postgres)
            .WithReference(kafka);

        builder.AddProject<Projects.SearchIndexer>("search-indexer")
            .WithReference(postgres)
            .WithReference(kafka)
            .WithReference(ecmHost);

        builder.AddProject<Projects.OutboxDispatcher>("outbox-dispatcher")
            .WithReference(postgres)
            .WithReference(kafka)
            .WithReference(ecmHost);

        builder.AddProject<Projects.Notify>("notify")
            .WithReference(kafka);

        builder.AddProject<Projects.Audit>("audit")
            .WithReference(postgres)
            .WithReference(kafka);

        builder.AddProject<Projects.Retention>("retention")
            .WithReference(postgres)
            .WithReference(kafka);

        builder.Build().Run();
    }
}
