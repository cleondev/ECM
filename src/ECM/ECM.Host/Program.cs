using ECM.Abstractions;
using ECM.Document.Api;
using ECM.File.Api;
using ECM.Host.Swagger;
using ECM.IAM.Api;
using ECM.SearchIndexer.Infrastructure;
using ECM.SearchRead.Api;
using ECM.Signature.Api;
using ECM.Workflow.Api;
using ECM.Ocr.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using ECM.IAM.Api.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
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
        builder.AddModule<OcrModule>();

        builder.Services.AddSearchIndexerInfrastructure();

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

        builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.Events ??= new JwtBearerEvents();
            var previousHandler = options.Events.OnTokenValidated;

            options.Events.OnTokenValidated = async context =>
            {
                if (previousHandler is not null)
                {
                    await previousHandler(context);
                }

                var provisioningService = context.HttpContext.RequestServices
                    .GetRequiredService<IUserProvisioningService>();

                await provisioningService.EnsureUserExistsAsync(
                    context.Principal,
                    context.HttpContext.RequestAborted);
            };
        });

        builder.Services.AddAuthorization();

        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: 'Authorization: Bearer {token}'",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            options.OperationFilter<MinimalApiParameterOperationFilter>();
        });

        var app = builder.Build();

        app.UseSerilogEnrichedRequestLogging();

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
        app.UseAntiforgery();

        app.MapDefaultEndpoints();
        app.MapModules();

        app.Run();
    }
}
