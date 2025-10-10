using ECM.Abstractions;
using ECM.Signature.Api.Requests;
using ECM.Signature.Application;
using ECM.Signature.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.Signature.Api;

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
