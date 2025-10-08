using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// TODO: Replace with .NET Aspire DistributedApplication configuration when Aspire packages are added.
// The placeholders below document the intended wiring between projects.
var host = builder.Build();

Console.WriteLine("AppHost placeholder ready. Configure DistributedApplication here.");

host.Run();
