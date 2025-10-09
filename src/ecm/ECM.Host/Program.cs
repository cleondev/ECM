using ECM.Modules.Abstractions;
using ECM.Modules.Document.Api;
using ECM.Modules.File.Api;
using ECM.Modules.SearchRead.Api;
using ECM.Modules.Signature.Api;
using ECM.Modules.Workflow.Api;
using ServiceDefaults;

namespace ECM.Host;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();
        builder.AddModule<DocumentModule>();
        builder.AddModule<FileModule>();
        builder.AddModule<WorkflowModule>();
        builder.AddModule<SignatureModule>();
        builder.AddModule<SearchReadModule>();

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
