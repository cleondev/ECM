using ECM.Abstractions;
using ECM.Document.Api.Documents;
using ECM.Document.Application;
using ECM.Document.Infrastructure;

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.Document.Api;

public sealed class DocumentModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDocumentApplication();
        services.AddDocumentInfrastructure();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDocumentEndpoints();
    }
}
