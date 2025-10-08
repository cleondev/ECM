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

app.MapGet("/api/ecm", () => Results.Ok(new { message = "ECM API placeholder" }))
   .WithName("GetEcmStatus")
   .WithDescription("Return a placeholder response for the ECM API.");

app.MapPost("/api/ecm/documents", (DocumentCreateRequest request) =>
{
    var document = new DocumentSummary(Guid.NewGuid(), request.Name, DateTimeOffset.UtcNow);
    return Results.Created($"/api/ecm/documents/{document.Id}", document);
}).WithName("CreateDocument")
  .WithDescription("Create a placeholder document and return a fake identifier.");

app.Run();

record DocumentCreateRequest(string Name);
record DocumentSummary(Guid Id, string Name, DateTimeOffset CreatedAt);
