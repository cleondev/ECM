using ECM.Abstractions;
using ECM.Signature.Api.Requests;
using ECM.Signature.Application;
using ECM.Signature.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace ECM.Signature.Api;

public sealed class SignatureModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSignatureApplication();
        services.AddSignatureInfrastructure();
        services.ConfigureModuleSwagger(SignatureSwagger.DocumentName, SignatureSwagger.Info);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapSignatureEndpoints();
    }
}

internal static class SignatureSwagger
{
    internal const string DocumentName = "signature";

    internal static readonly OpenApiInfo Info = new()
    {
        Title = "Signature API",
        Version = "v1",
        Description = "Digital signature orchestration and request handling endpoints."
    };
}
