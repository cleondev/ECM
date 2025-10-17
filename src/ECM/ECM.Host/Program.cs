using ECM.Abstractions;
using ECM.IAM.Api;
using ECM.Document.Api;
using ECM.File.Api;
using ECM.SearchIndexer.Infrastructure;
using ECM.SearchRead.Api;
using ECM.Signature.Api;
using ECM.Workflow.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Microsoft.Extensions.Options;
using ServiceDefaults;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ECM.Host;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddUserSecrets(typeof(Program).Assembly, optional: true);
        }

        builder.Configuration.AddEnvironmentVariables();

        builder.AddServiceDefaults();
        builder.AddModule<IamModule>();
        builder.AddModule<DocumentModule>();
        builder.AddModule<FileModule>();
        builder.AddModule<WorkflowModule>();
        builder.AddModule<SignatureModule>();
        builder.AddModule<SearchReadModule>();

        builder.Services.AddSearchIndexerInfrastructure();

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

            var swaggerGenOptions = app.Services.GetRequiredService<IOptions<SwaggerGenOptions>>();

            app.UseSwaggerUI(options =>
            {
                foreach (var (documentName, documentInfo) in swaggerGenOptions.Value.SwaggerGeneratorOptions.SwaggerDocs)
                {
                    var endpointUrl = $"/swagger/{documentName}/swagger.json";
                    var displayName = string.IsNullOrWhiteSpace(documentInfo.Title)
                        ? documentName
                        : documentInfo.Title;

                    options.SwaggerEndpoint(endpointUrl, displayName);
                }
            });
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
