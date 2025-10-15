using ECM.Abstractions;
using ECM.AccessControl.Api;
using ECM.Document.Api;
using ECM.File.Api;
using ECM.SearchRead.Api;
using ECM.Signature.Api;
using ECM.Workflow.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web;
using ServiceDefaults;
using Serilog;

namespace ECM.Host;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddUserSecrets<Program>(optional: true);
        }

        builder.AddServiceDefaults();
        builder.AddModule<AccessControlModule>();
        builder.AddModule<DocumentModule>();
        builder.AddModule<FileModule>();
        builder.AddModule<WorkflowModule>();
        builder.AddModule<SignatureModule>();
        builder.AddModule<SearchReadModule>();

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

        builder.Services.AddAuthorization();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapDefaultEndpoints();
        app.MapModules();

        app.Run();
    }
}
