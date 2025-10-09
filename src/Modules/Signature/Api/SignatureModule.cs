using ECM.Modules.Abstractions;
using ECM.Modules.Signature.Api.Requests;
using ECM.Modules.Signature.Application;
using ECM.Modules.Signature.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.Modules.Signature.Api;

public sealed class SignatureModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSignatureApplication();
        services.AddSignatureInfrastructure();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapSignatureEndpoints();
    }
}
