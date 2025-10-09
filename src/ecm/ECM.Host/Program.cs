using ECM.Modules.Abstractions;
using ECM.Modules.Document.Api;
using ServiceDefaults;

namespace ECM.Host;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();
        builder.AddModule<DocumentModule>();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.MapDefaultEndpoints();
        app.MapModules();

        app.Run();
    }
}
