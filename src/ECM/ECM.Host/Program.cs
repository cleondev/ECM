using ECM.Abstractions;
using ECM.Abstractions.Security;
using ECM.Document.Api;
using ECM.File.Api;
using ECM.Host.Auth;
using ECM.Host.Middleware;
using ECM.Host.Security;
using ECM.Host.Swagger;
using ECM.IAM.Api;
using ECM.Ocr.Api;
using ECM.SearchIndexer.Infrastructure;
using ECM.SearchRead.Api;
using ECM.Signature.Api;
using ECM.Workflow.Api;

using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

using ServiceDefaults;

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
        builder.Services.AddHostAuthentication(builder.Configuration);
        builder.Services.AddAuthorization();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUserProvider, HttpContextCurrentUserProvider>();

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

            options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Description = "API Key header for on-behalf requests. Example: 'X-Api-Key: {key}'",
                Name = ApiKeyAuthenticationHandler.HeaderName,
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "ApiKey"
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
        app.UseMiddleware<PasswordLoginClaimPropagationMiddleware>();
        app.UseAuthorization();
        app.UseAntiforgery();

        app.MapDefaultEndpoints();
        app.MapModules();

        app.Run();
    }
}
