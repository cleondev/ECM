using ECM.Abstractions;
using ECM.SearchIndexer.Api;
using ServiceDefaults;

namespace SearchIndexer;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();
        builder.AddModule<SearchIndexerModule>();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapDefaultEndpoints();
        app.MapModules();

        await app.RunAsync();
    }
}
