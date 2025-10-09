using ServiceDefaults;

namespace SearchApi;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();

        var app = builder.Build();

        app.MapDefaultEndpoints();
        app.MapGet("/api/search", (string? q) => Results.Ok(new { query = q ?? string.Empty, results = Array.Empty<object>() }))
           .WithName("SearchDocuments")
           .WithDescription("Execute a placeholder hybrid search query.");

        app.Run();
    }
}
