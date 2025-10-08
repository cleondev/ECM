using Aspire.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

const string dashboardUrlVariable = "ASPNETCORE_URLS";
const string dashboardGrpcVariable = "ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL";
const string dashboardHttpVariable = "ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL";
const string allowUnsecuredTransportVariable = "ASPIRE_ALLOW_UNSECURED_TRANSPORT";
const string dashboardUnsecured = "ASPIRE_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS";
Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://localhost:18888");
Environment.SetEnvironmentVariable(dashboardUnsecured, "true");

if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(allowUnsecuredTransportVariable)))
{
    Environment.SetEnvironmentVariable(allowUnsecuredTransportVariable, "true");
}

var builder = DistributedApplication.CreateBuilder(args);

var dashboardDefaults = new Dictionary<string, string?>();

if (string.IsNullOrWhiteSpace(builder.Configuration[dashboardUrlVariable]))
{
    dashboardDefaults[dashboardUrlVariable] = "http://+:18888";
}

if (string.IsNullOrWhiteSpace(builder.Configuration[dashboardGrpcVariable])
    && string.IsNullOrWhiteSpace(builder.Configuration[dashboardHttpVariable]))
{
    dashboardDefaults[dashboardHttpVariable] = "http://localhost:4318";
}

if (dashboardDefaults.Count > 0)
{
    builder.Configuration.AddInMemoryCollection(dashboardDefaults);
}

var postgres = builder.AddConnectionString("postgres");
var kafka = builder.AddConnectionString("kafka");
var minio = builder.AddConnectionString("minio");

var documentServices = builder.AddProject<Projects.DocumentServices>("document-services")
    .WithReference(postgres)
    .WithReference(kafka)
    .WithReference(minio);

builder.AddProject<Projects.Ecm>("ecm")
    .WithReference(postgres)
    .WithReference(kafka);

builder.AddProject<Projects.FileServices>("file-services")
    .WithReference(minio)
    .WithReference(postgres);

builder.AddProject<Projects.Workflow>("workflow")
    .WithReference(postgres)
    .WithReference(kafka);

builder.AddProject<Projects.SearchApi>("search-api")
    .WithReference(postgres)
    .WithReference(kafka);

builder.AddProject<Projects.SearchIndexer>("search-indexer")
    .WithReference(postgres)
    .WithReference(kafka)
    .WithReference(documentServices);

builder.AddProject<Projects.OutboxDispatcher>("outbox-dispatcher")
    .WithReference(postgres)
    .WithReference(kafka)
    .WithReference(documentServices);

builder.AddProject<Projects.Notify>("notify")
    .WithReference(kafka);

builder.AddProject<Projects.Audit>("audit")
    .WithReference(postgres)
    .WithReference(kafka);

builder.AddProject<Projects.Retention>("retention")
    .WithReference(postgres)
    .WithReference(kafka);

builder.Build().Run();
