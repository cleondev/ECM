using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddDefaultConfiguration()
       .AddServiceDefaults();

var app = builder.Build();

app.MapGet("/api/workflows", () => Results.Ok(Array.Empty<object>()))
   .WithName("ListWorkflowDefinitions")
   .WithDescription("List available workflow definitions.");

app.Run();
