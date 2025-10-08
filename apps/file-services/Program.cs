using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/api/files/presign", () => Results.Ok(new { url = "https://minio.local/presigned" }))
   .WithName("GeneratePresignedUrl")
   .WithDescription("Return a placeholder presigned URL for uploads.");

app.Run();
