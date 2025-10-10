using ECM.Modules.Abstractions;
using ECM.Modules.Document.Api.Documents;

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.Modules.Document.Api;

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
