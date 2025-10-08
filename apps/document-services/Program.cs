using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();
app.MapGet("/api/documents", () => Results.Ok(Array.Empty<object>()))
   .WithName("ListDocuments")
   .WithDescription("List all documents available to the caller.");

app.Run();
