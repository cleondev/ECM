using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDashboard("dashboard");

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
