using Ecm.Presentation.Documents;
using ServiceDefaults;

namespace Ecm;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();

        builder.Services.AddApplication();
        builder.Services.AddInfrastructure();

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
        app.MapDocumentEndpoints();

        app.Run();
    }
}
