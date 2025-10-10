using ECM.Abstractions;
using ECM.Modules.AccessControl.Api;
using ECM.Document.Api;
using ECM.File.Api;
using ECM.SearchRead.Api;
using ECM.Signature.Api;
using ECM.Workflow.Api;
using ServiceDefaults;

namespace ECM.Host;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();
        builder.AddModule<AccessControlModule>();
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
