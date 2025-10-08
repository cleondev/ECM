using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddDefaultConfiguration()
       .AddServiceDefaults();

builder.Services.AddControllers();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapControllers();

app.Run();
