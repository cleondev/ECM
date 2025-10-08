using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/api/workflows", () => Results.Ok(Array.Empty<object>()))
   .WithName("ListWorkflowDefinitions")
   .WithDescription("List available workflow definitions.");

app.Run();
