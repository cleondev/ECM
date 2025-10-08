using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddDefaultConfiguration()
       .AddServiceDefaults();

var app = builder.Build();

app.MapGet("/api/search", (string? q) => Results.Ok(new { query = q ?? string.Empty, results = Array.Empty<object>() }))
   .WithName("SearchDocuments")
   .WithDescription("Execute a placeholder hybrid search query.");

app.Run();
