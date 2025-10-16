using ECM.Abstractions;
using ECM.Document.Api.Documents;
using ECM.Document.Api.Tags;
using ECM.Document.Application;
using ECM.Document.Infrastructure;

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace ECM.Document.Api;

public sealed class DocumentModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDocumentApplication();
        services.AddDocumentInfrastructure();
        services.ConfigureModuleSwagger(DocumentSwagger.DocumentName, DocumentSwagger.Info);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDocumentEndpoints();
        endpoints.MapTagEndpoints();
    }
}

internal static class DocumentSwagger
{
    internal const string DocumentName = "documents";

    internal static readonly OpenApiInfo Info = new()
    {
        Title = "Documents API",
        Version = "v1",
        Description = "Operations for managing ECM documents and their associated tags."
    };
}
