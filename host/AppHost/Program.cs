using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables(prefix: "ECM_")
    .AddCommandLine(args)
    .Build();

Console.WriteLine("ECM AppHost");
Console.WriteLine("------------");
Console.WriteLine("Connection string overview:");

foreach (var name in new[] { "postgres", "kafka", "minio" })
{
    var value = configuration.GetConnectionString(name);
    Console.WriteLine($" - {name}: {(string.IsNullOrWhiteSpace(value) ? "<not configured>" : value)}");
}

Console.WriteLine();
Console.WriteLine("Registered application projects:");

foreach (var project in new[]
         {
             "Ecm API",
             "Document Services API",
             "File Services API",
             "Workflow Worker",
             "Search API",
             "Search Indexer Worker",
             "Outbox Dispatcher Worker",
             "Notify Worker",
             "Audit Worker",
             "Retention Worker"
         })
{
    Console.WriteLine($" - {project}");
}

Console.WriteLine();
Console.WriteLine("This simplified host outputs configuration information instead of orchestrating services.");
