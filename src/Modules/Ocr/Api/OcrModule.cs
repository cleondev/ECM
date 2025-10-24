using ECM.Abstractions;
using ECM.Ocr.Api.Ocr;
using ECM.Ocr.Application;
using ECM.Ocr.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace ECM.Ocr.Api;

public sealed class OcrModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddOcrApplication();
        services.AddOcrInfrastructure();
        services.ConfigureModuleSwagger(OcrSwagger.DocumentName, OcrSwagger.Info);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapOcrEndpoints();
    }
}

internal static class OcrSwagger
{
    internal const string DocumentName = "ocr";

    internal static readonly OpenApiInfo Info = new()
    {
        Title = "OCR API",
        Version = "v1",
        Description = "Endpoints for interacting with the Dot OCR service.",
    };
}
