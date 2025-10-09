var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();
app.MapGet("/", () => Results.Json(new { service = "App Gateway", status = "ready" }));

app.Run();
